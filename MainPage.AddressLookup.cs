using Microsoft.Maui.Devices.Sensors;

namespace BudApka;

public partial class MainPage
{
    void AddMapOptions(
        VerticalStackLayout page,
        VerticalStackLayout locationOptions,
        VerticalStackLayout streetOptions,
        Func<AddressPlace?> region,
        Func<AddressPlace?> city,
        Func<Entry> street,
        Func<Entry> building,
        Func<Placemark, Task<bool>> selectPlace)
    {
        var streetFeedback = Body("", 14, true);
        var streetChoices = new VerticalStackLayout { Spacing = 8 };
        var locationFeedback = Body("", 14, true);
        var locationChoices = new VerticalStackLayout { Spacing = 8 };
        bool searching = false;
        streetOptions.Add(Body(
            "Введіть частину назви або впишіть вулицю вручну.", 14, true));
        streetOptions.Add(CreateAsyncButton("Показати підказки вулиць", false,
            () => FindAsync(false, streetFeedback, streetChoices), "search"));
        streetOptions.Add(streetFeedback);
        streetOptions.Add(streetChoices);
        locationOptions.Add(CreateAsyncButton("Визначити адресу за геолокацією", false,
            () => FindAsync(true, locationFeedback, locationChoices), "pin"));
        locationOptions.Add(locationFeedback);
        locationOptions.Add(locationChoices);
        street().TextChanged += (_, _) =>
        {
            streetChoices.Children.Clear();
            streetFeedback.Text = "";
        };
        HideEmptyFeedback(streetFeedback, streetChoices);
        HideEmptyFeedback(locationFeedback, locationChoices);

        static void HideEmptyFeedback(Label feedback, VerticalStackLayout choices)
        {
            feedback.IsVisible = false;
            feedback.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(Label.Text))
                {
                    feedback.IsVisible = !string.IsNullOrEmpty(feedback.Text);
                }
            };
            choices.IsVisible = false;
            choices.ChildAdded += (_, _) => choices.IsVisible = true;
            choices.ChildRemoved += (_, _) => choices.IsVisible = choices.Children.Count > 0;
        }

        async Task FindAsync(bool useLocation, Label feedback, VerticalStackLayout choices)
        {
            if (searching)
            {
                return;
            }

            string cityCode = city()?.Code ?? "";
            string streetQuery = street().Text?.Trim() ?? "";
            if (!useLocation && (cityCode.Length == 0 || streetQuery.Length < 3
                || ValidateStreet(streetQuery) != null))
            {
                feedback.Text = "Оберіть населений пункт і введіть хоча б три символи назви.";
                return;
            }

            searching = true;
            choices.Children.Clear();
            feedback.Text = "Шукаємо адресу…";
            try
            {
                Location[] locations;
                if (useLocation)
                {
                    var permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (permission != PermissionStatus.Granted)
                    {
                        feedback.Text = "Доступ не надано. Адресу можна заповнити у формі.";
                        return;
                    }

                    var request = new GeolocationRequest(
                        GeolocationAccuracy.High, TimeSpan.FromSeconds(12));
                    var location = await Geolocation.Default.GetLocationAsync(request);
                    locations = location == null ? [] : [location];
                }
                else
                {
                    string query = $"{streetQuery}, {city()?.Name}, {region()?.Name}, Україна";
                    var results = await Geocoding.Default.GetLocationsAsync(query)
                        .WaitAsync(TimeSpan.FromSeconds(12));
                    locations = results.Take(3).ToArray();
                }

                var candidates = new List<Placemark>();
                foreach (var location in locations)
                {
                    var places = await Geocoding.Default
                        .GetPlacemarksAsync(location.Latitude, location.Longitude)
                        .WaitAsync(TimeSpan.FromSeconds(8));
                    candidates.AddRange(places.Where(place =>
                        string.Equals(place.CountryCode, "UA", StringComparison.OrdinalIgnoreCase)
                        && (!string.IsNullOrWhiteSpace(place.Locality)
                            || !string.IsNullOrWhiteSpace(place.SubLocality))
                        && !string.IsNullOrWhiteSpace(place.Thoroughfare)
                        && (useLocation || string.Equals(
                            city()?.Name, place.Locality, StringComparison.OrdinalIgnoreCase))));
                }

                if (BodyHost.Content is not ScrollView scroll || scroll.Content != page
                    || (!useLocation && (city()?.Code != cityCode
                        || street().Text?.Trim() != streetQuery)))
                {
                    feedback.Text = "";
                    return;
                }

                var options = candidates.DistinctBy(place =>
                    (place.AdminArea, place.Locality, place.SubLocality, place.Thoroughfare,
                        useLocation ? place.SubThoroughfare : null))
                    .Take(5).ToArray();
                if (options.Length == 0)
                {
                    feedback.Text = "Адресу не знайдено. Заповніть її вручну або уточніть пошук.";
                    return;
                }

                feedback.Text = "Перевірте результат і виберіть потрібну вулицю.";
                foreach (var place in options)
                {
                    string locality = string.IsNullOrWhiteSpace(place.Locality)
                        ? place.SubLocality : place.Locality;
                    string address = useLocation && IsBuilding(place.SubThoroughfare ?? "")
                        ? $"{place.Thoroughfare}, {place.SubThoroughfare}" : place.Thoroughfare;
                    choices.Add(CreateAsyncButton($"{address} · {locality}", false, async () =>
                    {
                        if (useLocation && !await selectPlace(place))
                        {
                            feedback.Text = "Не вдалося зіставити населений пункт із довідником. "
                                + "Виберіть його у формі й перевірте адресу.";
                            return;
                        }
                        if (!useLocation && city()?.Code != cityCode)
                        {
                            feedback.Text = "Населений пункт змінився. Повторіть пошук вулиці.";
                            return;
                        }
                        if (BodyHost.Content != scroll)
                        {
                            return;
                        }

                        street().Text = place.Thoroughfare;
                        if (useLocation && IsBuilding(place.SubThoroughfare ?? ""))
                        {
                            building().Text = place.SubThoroughfare;
                        }
                        choices.Children.Clear();
                        feedback.Text = useLocation ? "Адресу заповнено. Перевірте поля нижче."
                            : "Вулицю вибрано. Перевірте дані перед продовженням.";
                    }));
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                feedback.Text = "Підказки недоступні. Адресу можна заповнити у формі.";
            }
            finally
            {
                searching = false;
            }
        }

    }
}
