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
