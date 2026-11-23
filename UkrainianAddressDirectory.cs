namespace BudApka;

internal sealed record AddressPlace(string RegionCode, string Code, string Name, string Community)
{
    public string DisplayName => Community.Length == 0 ? Name : $"{Name} · {Community} громада";
}

internal sealed record UkrainianAddressDirectory(
    AddressPlace[] Regions,
    ILookup<string, AddressPlace> Settlements,
    ILookup<string, AddressPlace> CityDistricts)
{
    private static readonly Dictionary<string, string> _regionAliases = new()
    {
        ["crimea"] = "UA01", ["vinnytsia"] = "UA05", ["volyn"] = "UA07",
        ["dnipropetrovsk"] = "UA12", ["donetsk"] = "UA14", ["zhytomyr"] = "UA18",
        ["zakarpattia"] = "UA21", ["zaporizhzhia"] = "UA23", ["ivanofrankivsk"] = "UA26",
        ["kyiv"] = "UA32", ["kiev"] = "UA32", ["kirovohrad"] = "UA35",
        ["luhansk"] = "UA44", ["lviv"] = "UA46", ["mykolaiv"] = "UA48",
        ["odesa"] = "UA51", ["odessa"] = "UA51", ["poltava"] = "UA53",
        ["rivne"] = "UA56", ["sumy"] = "UA59", ["ternopil"] = "UA61",
        ["kharkiv"] = "UA63", ["kherson"] = "UA65", ["khmelnytskyi"] = "UA68",
        ["cherkasy"] = "UA71", ["chernivtsi"] = "UA73", ["chernihiv"] = "UA74",
    };

    public AddressPlace? ResolveSettlement(string? locality, string? subLocality, string? adminArea)
    {
        string regionName = RegionKey(adminArea);
        string? regionPrefix = _regionAliases.GetValueOrDefault(regionName);
        string? regionCode = Regions.FirstOrDefault(place =>
            RegionKey(place.Name) == regionName
            || (regionPrefix != null && place.Code.StartsWith(regionPrefix,
                StringComparison.Ordinal)))?.Code;

        var matches = FindMatches(locality);
        if (matches.Length == 0)
        {
            matches = FindMatches(subLocality);
        }

        AddressPlace? specialCity = matches.FirstOrDefault(place =>
            place.Code.EndsWith("-city", StringComparison.Ordinal));
        if (specialCity != null)
        {
            return specialCity;
        }

        if (regionCode != null)
        {
            matches = matches.Where(place => place.RegionCode == regionCode).ToArray();
        }

        return matches.Length == 1 ? matches[0] : null;

        AddressPlace[] FindMatches(string? name)
        {
            string key = PlaceKey(name) == "kiev" ? "kyiv" : PlaceKey(name);
            if (key.Length == 0)
            {
                return [];
            }

            return Settlements.SelectMany(group => group)
                .Where(place => PlaceKey(place.Name) == key
                    || (key.All(char.IsAscii) && LatinPlaceKey(place.Name) == key)).ToArray();
        }
    }

    private static string RegionKey(string? name) => PlaceKey(name?.ToLowerInvariant()
        .Replace("область", "").Replace("oblast", "").Replace("region", ""));

    private static string PlaceKey(string? name) => name == null
        ? "" : string.Concat(name.ToLowerInvariant().Where(char.IsLetterOrDigit));

    private static string LatinPlaceKey(string name) =>
        string.Concat(name.ToLowerInvariant().Select((character, index) => character switch
        {
            'а' => "a", 'б' => "b", 'в' => "v", 'г' => "h", 'ґ' => "g",
            'д' => "d", 'е' => "e", 'є' => (index == 0 || !char.IsLetter(name[index - 1])) ? "ye" : "ie",
            'ж' => "zh", 'з' => "z", 'и' => "y", 'і' => "i",
            'ї' => (index == 0 || !char.IsLetter(name[index - 1])) ? "yi" : "i", 'й' => (index == 0 || !char.IsLetter(name[index - 1])) ? "y" : "i",
            'к' => "k", 'л' => "l", 'м' => "m", 'н' => "n", 'о' => "o", 'п' => "p",
            'р' => "r", 'с' => "s", 'т' => "t", 'у' => "u", 'ф' => "f", 'х' => "kh",
            'ц' => "ts", 'ч' => "ch", 'ш' => "sh", 'щ' => "shch",
            'ю' => (index == 0 || !char.IsLetter(name[index - 1])) ? "yu" : "iu", 'я' => (index == 0 || !char.IsLetter(name[index - 1])) ? "ya" : "ia",
            _ => "",
        }));

    public static async Task<UkrainianAddressDirectory> LoadAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("UkrainianSettlements.txt")
            .ConfigureAwait(false);
        using var reader = new StreamReader(stream);
        var places = new List<AddressPlace>();
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            string[] parts = line.Split('\t');
            if (parts.Length == 4)
            {
                places.Add(new(parts[0], parts[1], parts[2], parts[3]));
            }
        }

        using var districtStream = await FileSystem
            .OpenAppPackageFileAsync("UkrainianCityDistricts.txt").ConfigureAwait(false);
        using var districtReader = new StreamReader(districtStream);
        var districts = new List<AddressPlace>();
        while (await districtReader.ReadLineAsync().ConfigureAwait(false) is { } districtLine)
        {
            string[] parts = districtLine.Split('\t');
            if (parts.Length == 3)
            {
                districts.Add(new(parts[0], parts[1], parts[2], ""));
            }
        }

        return new(
            places.Where(place => place.RegionCode == place.Code).OrderBy(place => place.Name)
                .ToArray(),
            places.Where(place => place.RegionCode != place.Code).OrderBy(place => place.Name)
                .ToLookup(place => place.RegionCode),
            districts.OrderBy(place => place.Name).ToLookup(place => place.RegionCode));
    }
}
