using System.Text.RegularExpressions;
using Microsoft.Maui.Devices.Sensors;

namespace BudApka;

public partial class MainPage
{
    UkrainianAddressDirectory? _addressDirectory;
    Func<bool>? _validateAddressSelection;

    void AddAddressFields(VerticalStackLayout page)
    {
        var locationOptions = new VerticalStackLayout { Spacing = 8 };
        page.Add(locationOptions);
        var streetOptions = new VerticalStackLayout { Spacing = 8 };
        var details = new VerticalStackLayout { Spacing = 8, IsVisible = false };
        var streetHint = Body("Щоб вказати вулицю, спочатку оберіть населений пункт.",
            14, true);
        Entry? street = null;
        Entry? building = null;
        var initial = new WorkArea(_viewModel.RegionCode, _viewModel.SettlementCode,
            _viewModel.CityDistrictCode, _viewModel.District);
        var controls = AddAreaFields(page, initial, area =>
        {
            if (BodyHost.Content is not ScrollView scroll || scroll.Content != page)
            {
                return;
            }

            bool changed = area.RegionCode != _viewModel.RegionCode
                || area.SettlementCode != _viewModel.SettlementCode;
            _viewModel.RegionCode = area.RegionCode;
            _viewModel.SettlementCode = area.SettlementCode;
            _viewModel.CityDistrictCode = area.DistrictCode;
            _viewModel.District = area.DisplayName;
            bool hasSettlement = area.SettlementCode.Length > 0;
            details.IsVisible = hasSettlement;
            streetHint.IsVisible = !hasSettlement;
            if (changed && street != null && building != null)
            {
                street.Text = "";
                building.Text = "";
            }
        }, requireSettlement: true);
        _validateAddressSelection = controls.Validate;

        details.Add(LabeledField(
            "Вулиця", _viewModel.Street, "Почніть із назви вулиці",
            "Вулицю й номер будинку побачить обраний майстер.",
            value => _viewModel.Street = value,
            validate: ValidateStreet,
            allowCharacter: c => InputRules.AddressCharacter(c) && c != ',',
            maxLength: 100, configureEntry: entry =>
            {
                street = entry;
                entry.ReturnType = ReturnType.Next;
                entry.Completed += (_, _) => ReviewAddress();
            }));
        details.Add(streetOptions);
        details.Add(LabeledField(
            "Номер будинку · необов’язково", _viewModel.Building, "Наприклад, 12А",
            "Номер квартири можна повідомити майстру після узгодження часу візиту.",
            value => _viewModel.Building = value,
            validate: value => string.IsNullOrWhiteSpace(value) || IsBuilding(value)
                ? null : "Вкажіть лише номер будинку, наприклад 12А або 12/1.",
            allowCharacter: c => char.IsLetter(c) || InputRules.Digit(c) || c is '/' or '-',
            maxLength: 16, configureEntry: entry => building = entry));
        page.Add(streetHint);
        page.Add(details);

        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS()
            || OperatingSystem.IsMacCatalyst())
        {
            AddMapOptions(page, locationOptions, streetOptions, () => controls.Region.SelectedItem as AddressPlace,
                () => controls.City.SelectedItem as AddressPlace, () => street!, () => building!,
                controls.SelectMapPlace);
        }
    }

    (Picker Region, Picker City, Func<bool> Validate, Func<Placemark, Task<bool>> SelectMapPlace)
        AddAreaFields(VerticalStackLayout page, WorkArea initial, Action<WorkArea> changed,
            bool requireSettlement, string fixedRegionCode = "")
    {
        var region = AddressPicker("Оберіть регіон");
        var city = AddressPicker(requireSettlement ? "Оберіть населений пункт"
            : "Увесь регіон або населений пункт");
        var citySearch = Field("Назва міста, селища або села", _ => { });
        var district = AddressPicker("Увесь населений пункт");
        var districtLabel = Body("Район міста · необов’язково", 14);
        var status = Body("", 14, true);
        var regionWarning = ValidationMessage();
        var cityWarning = ValidationMessage();
        var cityHint = Body("Спочатку оберіть область або місто зі спеціальним статусом.",
            14, true);
        var clearCity = CreateButton("Увесь регіон", false, () => city.SelectedItem = null);
        var clearDistrict = CreateButton("Увесь населений пункт", false,
            () => district.SelectedItem = null);
        bool updating = false;
        bool loaded = false;
        Task? directoryLoad = null;
        page.Add(Body("Область або місто зі спеціальним статусом", 14));
        page.Add(region);
        page.Add(regionWarning);
        page.Add(Body("Населений пункт", 14));
        page.Add(cityHint);
        var searchRow = WithIcon("search", citySearch);
        page.Add(searchRow);
        page.Add(city);
        page.Add(cityWarning);
        page.Add(clearCity);
        page.Add(districtLabel);
        page.Add(district);
        page.Add(clearDistrict);
        page.Add(status);
        region.IsEnabled = false;
        RefreshVisibility();

        region.SelectedIndexChanged += (_, _) =>
        {
            if (updating)
            {
                return;
            }
            regionWarning.IsVisible = false;
            cityWarning.IsVisible = false;
            region.BackgroundColor = ThemeColor("#FFFFFF", "#202833");
            updating = true;
            citySearch.Text = "";
            updating = false;
            RefreshCities("", "");
        };
        citySearch.TextChanged += (_, _) =>
        {
            if (!updating)
            {
                RefreshCities((city.SelectedItem as AddressPlace)?.Code ?? "",
                    (district.SelectedItem as AddressPlace)?.Code ?? "");
            }
        };
        city.SelectedIndexChanged += (_, _) =>
        {
            if (!updating)
            {
                cityWarning.IsVisible = false;
                RefreshDistricts("");
                Publish();
            }
        };
        district.SelectedIndexChanged += (_, _) =>
        {
            if (!updating)
            {
                Publish();
            }
        };
        var loading = WithIcon("loader", Body("Завантажуємо довідник…", 14, true));
        var error = new VerticalStackLayout { Spacing = 8, IsVisible = false };
        error.Add(StateBadge("Помилка", ThemeColor("#B33232", "#FFB4AB"),
            ThemeColor("#FCEDED", "#542725")));
        error.Add(Body("Не вдалося відкрити довідник населених пунктів.", 14));
        error.Add(CreateAsyncButton("Повторити", false, LoadDirectoryAsync));
        page.Add(loading);
        page.Add(error);
        page.Loaded += async (_, _) => await LoadDirectoryAsync();

        Task LoadDirectoryAsync()
        {
            if (loaded)
            {
                return Task.CompletedTask;
            }

            if (directoryLoad is { IsCompleted: false })
            {
                return directoryLoad;
            }

            directoryLoad = LoadDirectoryCoreAsync();
            return directoryLoad;
        }

        async Task LoadDirectoryCoreAsync()
        {
            loading.IsVisible = true;
            error.IsVisible = false;
            status.Text = "";
            try
            {
                _addressDirectory ??= await UkrainianAddressDirectory.LoadAsync();
                loaded = true;
                updating = true;
                region.ItemsSource = _addressDirectory.Regions;
                region.SelectedItem = _addressDirectory.Regions.FirstOrDefault(
                    item => item.Code == (fixedRegionCode.Length > 0 ? fixedRegionCode
                        : initial.RegionCode));
                region.IsEnabled = fixedRegionCode.Length == 0;
                updating = false;
                RefreshCities(initial.SettlementCode, initial.DistrictCode);
            }
            catch (IOException)
            {
                error.IsVisible = true;
            }
            finally
            {
                loading.IsVisible = false;
            }
        }

        return (region, city, Validate, SelectMapPlace);

        void ShowWarning(string message, Picker target)
        {
            var warning = target == region ? regionWarning : cityWarning;
            warning.Text = message;
            warning.IsVisible = true;
            if (target == region)
            {
                region.BackgroundColor = ThemeColor("#FFE4E0", "#542725");
            }
            ScrollToValidation(warning);
        }

        bool Validate()
        {
            if (!loaded)
            {
                ScrollToValidation(loading.IsVisible ? loading : error);
                return false;
            }
            if (region.SelectedItem == null)
            {
                ShowWarning("Спочатку оберіть регіон.", region);
                return false;
            }
            if (requireSettlement && city.SelectedItem == null)
            {
                ShowWarning("Оберіть населений пункт зі списку.", city);
                return false;
            }
            return true;
        }

        void RefreshVisibility()
        {
            bool hasRegion = region.SelectedItem != null;
            cityHint.IsVisible = !hasRegion;
            searchRow.IsVisible = hasRegion;
            city.IsVisible = hasRegion;
            clearCity.IsVisible = !requireSettlement && city.SelectedItem != null;
            bool hasDistricts = district.ItemsSource?.Count > 0;
            districtLabel.IsVisible = hasDistricts;
            district.IsVisible = hasDistricts;
            clearDistrict.IsVisible = district.SelectedItem != null;
        }

        void RefreshCities(string cityCode, string districtCode)
        {
            updating = true;
            if (_addressDirectory != null && region.SelectedItem is AddressPlace selected)
            {
                var settlements = _addressDirectory.Settlements[selected.Code];
                var current = settlements.FirstOrDefault(item => item.Code == cityCode);
                string query = (citySearch.Text ?? "").Trim();
                var matches = settlements.Where(item =>
                    item.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                    .Take(81).ToList();
                bool limited = matches.Count > 80;
                if (limited)
                {
                    matches.RemoveAt(80);
                }
                if (current != null && !matches.Contains(current))
                {
                    matches.Insert(0, current);
                }
                city.ItemsSource = matches;
                city.SelectedItem = current;
                city.IsEnabled = matches.Count > 0;
                status.Text = matches.Count == 0 ? "Нічого не знайдено. Уточніть назву."
                    : limited ? "Введіть частину назви, щоб звузити список." : "";
            }
            else
            {
                city.ItemsSource = null;
                city.SelectedItem = null;
                status.Text = "";
            }
            updating = false;
            RefreshDistricts(districtCode);
            Publish();
        }

        void RefreshDistricts(string code)
        {
            updating = true;
            var selectedCity = city.SelectedItem as AddressPlace;
            var districts = selectedCity == null ? Array.Empty<AddressPlace>()
                : _addressDirectory!.CityDistricts[selectedCity.Code].ToArray();
            district.ItemsSource = districts;
            district.SelectedItem = districts.FirstOrDefault(item => item.Code == code);
            updating = false;
        }

        void Publish()
        {
            RefreshVisibility();
            var selectedRegion = region.SelectedItem as AddressPlace;
            var selectedCity = city.SelectedItem as AddressPlace;
            var selectedDistrict = district.SelectedItem as AddressPlace;
            var words = new[] { selectedCity?.DisplayName, selectedDistrict?.Name,
                selectedRegion?.Name }.Where(word => !string.IsNullOrEmpty(word)).Distinct();
            changed(new WorkArea(selectedRegion?.Code ?? "", selectedCity?.Code ?? "",
                selectedDistrict?.Code ?? "", string.Join(" · ", words)));
        }

        async Task<bool> SelectMapPlace(Placemark place)
        {
            await LoadDirectoryAsync();
            if (!loaded || _addressDirectory == null
                || BodyHost.Content is not ScrollView scroll || scroll.Content != page)
            {
                return false;
            }

            var target = _addressDirectory.ResolveSettlement(
                place.Locality, place.SubLocality, place.AdminArea);
            if (target == null)
            {
                return false;
            }
            if ((city.SelectedItem as AddressPlace)?.Code == target.Code)
            {
                return true;
            }

            regionWarning.IsVisible = false;
            cityWarning.IsVisible = false;
            region.BackgroundColor = ThemeColor("#FFFFFF", "#202833");
            updating = true;
            region.SelectedItem = _addressDirectory.Regions
                .First(item => item.Code == target.RegionCode);
            citySearch.Text = target.Name;
            updating = false;
            RefreshCities(target.Code, "");
            return true;
        }
    }

    Picker AddressPicker(string title) => new()
    {
        Title = title,
        ItemDisplayBinding = new Binding(nameof(AddressPlace.DisplayName)),
        TextColor = ThemeColor("#202823", "#F4F7FB"),
        TitleColor = ThemeColor("#566257", "#BFCADA"),
        BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
        FontFamily = "Inter",
        FontSize = 16,
        MinimumHeightRequest = 52,
    };

    static bool IsBuilding(string value) =>
        Regex.IsMatch(value.Trim(), @"\A[0-9]+\p{L}?(?:[-/][0-9]+\p{L}?)?\z");

    static string? ValidateStreet(string value)
    {
        string street = value.Trim();
        if (!street.Any(char.IsLetter))
        {
            return "Вкажіть назву вулиці.";
        }
        if (Regex.IsMatch(street, @"(?:\s|№)(?:кв\.?|квартира|буд\.?|будинок)\s*[0-9]",
            RegexOptions.IgnoreCase) || Regex.IsMatch(street, @"\s[0-9]+\p{L}?\z"))
        {
            return "Введіть лише назву. Номер будинку має окреме поле.";
        }

        return null;
    }
}
