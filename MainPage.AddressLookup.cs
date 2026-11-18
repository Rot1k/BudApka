using Microsoft.Maui.Devices.Sensors;

namespace BudApka;

public partial class MainPage
{
    void AddMapOptions(
        VerticalStackLayout page,
        VerticalStackLayout details,
        Func<AddressPlace?> region,
        Func<AddressPlace?> city,
        Func<Entry> street,
        Func<Entry> building,
        Func<Placemark, bool> selectPlace)
    {
        var feedback = Body("", 14, true);
        var choices = new VerticalStackLayout { Spacing = 8 };
        bool searching = false;
        details.Insert(1, CreateAsyncButton("Показати підказки вулиць", false,
            () => FindAsync(false)));
        details.Insert(2, Body(
            "Введіть частину назви й виберіть результат. Якщо вулиці немає, впишіть її вручну.",
            14, true));
        page.Add(feedback);
        page.Add(choices);
        page.Add(CreateAsyncButton("Визначити адресу за геолокацією", false, () => FindAsync(true)));
        street().TextChanged += (_, _) =>
        {
            choices.Children.Clear();
            feedback.Text = "";
        };

        async Task FindAsync(bool useLocation)
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
                        && !string.IsNullOrWhiteSpace(place.Locality)
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

                var options = candidates.DistinctBy(place => (place.Locality, place.Thoroughfare))
                    .Take(5).ToArray();
                if (options.Length == 0)
                {
                    feedback.Text = "Підказок не знайдено. Назву вулиці можна ввести вручну.";
                    return;
                }

                feedback.Text = "Перевірте результат і виберіть потрібну вулицю.";
                foreach (var place in options)
                {
                    choices.Add(CreateButton(
                        $"{place.Thoroughfare} · {place.Locality}", false, () =>
                        {
                            if (useLocation ? !selectPlace(place) : city()?.Code != cityCode)
                            {
                                feedback.Text = "Уточніть населений пункт і повторіть пошук.";
                                return;
                            }

                            street().Text = place.Thoroughfare;
                            if (useLocation && IsBuilding(place.SubThoroughfare ?? ""))
                            {
                                building().Text = place.SubThoroughfare;
                            }
                            choices.Children.Clear();
                            feedback.Text = "Вулицю вибрано. Перевірте дані перед продовженням.";
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
