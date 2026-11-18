using System.Text.RegularExpressions;
using Microsoft.Maui.Devices.Sensors;

namespace BudApka;

public partial class MainPage
{
    UkrainianAddressDirectory? _addressDirectory;
    Func<bool>? _validateAddressSelection;

    void AddAddressFields(VerticalStackLayout page)
    {
        var region = AddressPicker("Оберіть регіон");
        var city = AddressPicker("Оберіть населений пункт");
        var citySearch = Field("Назва міста, селища або села", _ => { });
        var status = Body("Завантажуємо довідник населених пунктів…", 14, true);
        var details = new VerticalStackLayout { Spacing = 8, IsEnabled = false };
        Entry? street = null;
        Entry? building = null;
        bool updating = false;

        page.Add(Body("Область або місто зі спеціальним статусом", 14));
        page.Add(region);
        page.Add(Body("Населений пункт", 14));
        page.Add(citySearch);
        page.Add(city);
        page.Add(status);

        details.Add(LabeledField(
            "Вулиця", _viewModel.Street, "Почніть із назви вулиці",
            "Вулицю й номер будинку побачить обраний майстер.",
            value => _viewModel.Street = value,
            validate: ValidateStreet,
            allowCharacter: c => InputRules.AddressCharacter(c) && c != ',',
            maxLength: 100, configureEntry: entry => street = entry));
        details.Add(LabeledField(
            "Номер будинку · необов’язково", _viewModel.Building, "Наприклад, 12А",
            "Номер квартири можна повідомити майстру після узгодження часу візиту.",
            value => _viewModel.Building = value,
            validate: value => string.IsNullOrWhiteSpace(value) || IsBuilding(value)
                ? null : "Вкажіть лише номер будинку, наприклад 12А або 12/1.",
            allowCharacter: c => char.IsLetter(c) || InputRules.Digit(c) || c is '/' or '-',
            maxLength: 16, configureEntry: entry => building = entry));
        page.Add(details);

        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS()
            || OperatingSystem.IsMacCatalyst())
        {
            AddMapOptions(page, details, () => region.SelectedItem as AddressPlace,
                () => city.SelectedItem as AddressPlace, () => street!, () => building!,
                SelectMapPlace);
        }

        _validateAddressSelection = () =>
        {
            if (region.SelectedItem is AddressPlace && city.SelectedItem is AddressPlace)
            {
                return true;
            }

            if (_addressDirectory != null)
            {
                status.Text = region.SelectedItem == null
                    ? "Спочатку оберіть область." : "Оберіть населений пункт зі списку.";
            }
            if (BodyHost.Content is ScrollView scroll)
            {
                _ = scroll.ScrollToAsync(region, ScrollToPosition.Center, false);
            }
            return false;
        };

        region.SelectedIndexChanged += (_, _) =>
        {
            if (updating)
            {
                return;
            }
            _viewModel.RegionCode = (region.SelectedItem as AddressPlace)?.Code ?? "";
            _viewModel.SettlementCode = "";
            _viewModel.District = "";
            street!.Text = "";
            building!.Text = "";
            citySearch.Text = "";
            RefreshCities();
        };
        citySearch.TextChanged += (_, _) => RefreshCities();
        city.SelectedIndexChanged += (_, _) =>
        {
            if (updating || city.SelectedItem is not AddressPlace selected
                || region.SelectedItem is not AddressPlace selectedRegion)
            {
                return;
            }

            if (selected.Code != _viewModel.SettlementCode)
            {
                street!.Text = "";
                building!.Text = "";
            }
            _viewModel.SettlementCode = selected.Code;
            _viewModel.District = selected.Name == selectedRegion.Name
                ? selected.Name : $"{selected.DisplayName} · {selectedRegion.Name}";
            details.IsEnabled = true;
            status.Text = "";
        };
        page.Loaded += async (_, _) =>
        {
            try
            {
                _addressDirectory ??= await UkrainianAddressDirectory.LoadAsync();
                updating = true;
                region.ItemsSource = _addressDirectory.Regions;
                region.SelectedItem = _addressDirectory.Regions
                    .FirstOrDefault(item => item.Code == _viewModel.RegionCode);
                region.IsEnabled = true;
                updating = false;
                RefreshCities();
            }
            catch (IOException)
            {
                status.Text = "Довідник недоступний. Поверніться назад і відкрийте адресу знову.";
            }
        };
        region.IsEnabled = false;
        city.IsEnabled = false;
        citySearch.IsEnabled = false;

        bool SelectMapPlace(Placemark place)
        {
            if (_addressDirectory == null || string.IsNullOrWhiteSpace(place.Locality))
            {
                return false;
            }

            var matches = _addressDirectory.Settlements
                .SelectMany(group => group)
                .Where(item => item.Name.Equals(place.Locality, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var target = matches.Length == 1 ? matches[0] : matches.FirstOrDefault(item =>
                item.RegionCode == (region.SelectedItem as AddressPlace)?.Code
                && item.Code == (city.SelectedItem as AddressPlace)?.Code);
            if (target == null)
            {
                return false;
            }

            region.SelectedItem = _addressDirectory.Regions
                .First(item => item.Code == target.RegionCode);
            citySearch.Text = target.Name;
            city.SelectedItem = target;
            return true;
        }

        void RefreshCities()
        {
            if (_addressDirectory == null || region.SelectedItem is not AddressPlace selected)
            {
                city.ItemsSource = null;
                city.IsEnabled = false;
                citySearch.IsEnabled = false;
                details.IsEnabled = false;
                status.Text = "Оберіть область, щоб побачити її населені пункти.";
                return;
            }

            var settlements = _addressDirectory.Settlements[selected.Code];
            var current = settlements.FirstOrDefault(item => item.Code == _viewModel.SettlementCode);
            string query = (citySearch.Text ?? "").Trim();
            var matches = settlements.Where(item =>
                item.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)).Take(81)
                .ToList();
            bool limited = matches.Count > 80;
            if (limited)
            {
                matches.RemoveAt(80);
            }
            if (current != null && !matches.Contains(current))
            {
                matches.Insert(0, current);
            }
            updating = true;
            city.ItemsSource = matches;
            city.SelectedItem = current;
            updating = false;
            city.IsEnabled = matches.Count > 0;
            citySearch.IsEnabled = true;
            details.IsEnabled = current != null;
            status.Text = matches.Count == 0 ? "Нічого не знайдено. Уточніть назву."
                : limited ? "Введіть частину назви, щоб звузити список." : "";
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
