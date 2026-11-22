namespace BudApka;

public sealed record WorkArea(string RegionCode, string SettlementCode, string DistrictCode,
    string DisplayName)
{
    public static WorkArea Empty { get; } = new("", "", "", "");

    public bool Includes(WorkArea place) => RegionCode == place.RegionCode
        && (SettlementCode.Length == 0 || SettlementCode == place.SettlementCode)
        && (DistrictCode.Length == 0 || DistrictCode == place.DistrictCode);
}

public sealed record RepairRequest(int Id, string CategoryId, string Category, string Title,
    string Description, string Symptoms, string Budget, WorkArea Area, EquipmentRecord? Equipment,
    string PhotoPath = "");
