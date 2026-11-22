using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BudApka;

public enum AccountMode
{
    Client,
    Master,
}

public enum OrderStatus
{
    Draft,
    Submitted,
    HasResponses,
    MasterSelected,
    VisitRecorded,
    Completed,
    Cancelled,
}

public sealed record MasterOffer(
    string Id,
    string Name,
    string Specialty,
    int Years,
    string Message,
    string Price,
    string Rating,
    string Reviews,
    string PhotoAsset
);

public sealed record ChatMessage(string Text, string Time, bool FromClient);

public sealed record EquipmentRecord(string Name, string Type, string Model, string Notes = "");

internal sealed class PrototypeViewModel : INotifyPropertyChanged
{
    private readonly Dictionary<int, RepairOrder> _orders = new();
    // The built-in sample requests use numbers 2051 and 2052.
    private int _nextRequestId = 2053;

    string _screen = "Home";
    AccountMode _mode = AccountMode.Client;
    public event PropertyChangedEventHandler? PropertyChanged;
    public string CurrentScreen
    {
        get => _screen;
        set
        {
            if (_screen == value)
                return;
            _screen = value;
            OnPropertyChanged();
        }
    }
    public AccountMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value)
                return;
            _mode = value;
            OnPropertyChanged();
        }
    }
    public string CategoryId { get; private set; } = "";
    public string OtherCategory { get; set; } = "";
    public string Category => CategoryId == RepairCatalog.Other
        ? OtherCategory.Trim() : RepairCatalog.Find(CategoryId)?.Name ?? "";
    public HashSet<string> Symptoms { get; } = new(StringComparer.Ordinal);
    public string Description { get; set; } = "";
    public EquipmentRecord? DraftEquipment { get; set; }

    public void SelectCategory(string id)
    {
        if (CategoryId == id)
        {
            return;
        }

        CategoryId = id;
        Symptoms.Clear();
    }

    public string? DraftError => CategoryId.Length == 0 ? "Оберіть категорію ремонту."
        : Category.Length == 0 ? "Вкажіть назву вашої категорії."
        : Symptoms.Count == 0 && string.IsNullOrWhiteSpace(Description)
            ? "Оберіть підказку або опишіть завдання своїми словами." : null;

    public string PhotoPath { get; set; } = "";
    public string RegionCode { get; set; } = "";
    public string SettlementCode { get; set; } = "";
    public string CityDistrictCode { get; set; } = "";
    public string District { get; set; } = "";
    public string Street { get; set; } = "";
    public string Building { get; set; } = "";
    public string ExactAddress => (Street.Trim() + " " + Building.Trim()).Trim();
    public string Budget { get; set; } = "1500";
    public bool SaveAddress { get; set; }
    public string Phone { get; set; } = "";
    public string Code { get; set; } = "";
    public string ProfileName { get; set; } = "";
    public bool IsSignedIn { get; set; }
    public bool HasRequestDraft { get; set; }
    public bool HasOrder => _orders.Count > 0;
    public IEnumerable<RepairOrder> Orders =>
        _orders.Values.OrderByDescending(order => order.Request.Id);
    public RepairOrder? CurrentOrder { get; private set; }

    public RepairOrder? FindOrder(int requestId) => _orders.GetValueOrDefault(requestId);

    public void OpenOrder(RepairOrder order, string screen)
    {
        CurrentOrder = order;
        CurrentScreen = screen;
    }

    public string SavedAddress { get; private set; } = "";
    public const string LocalMasterId = "local-master";
    public bool CanChat(RepairOrder order) => order.SelectedMaster != null
        && (Mode == AccountMode.Client || order.IsLocalMasterSelected)
        && order.Status is OrderStatus.MasterSelected or OrderStatus.VisitRecorded;

    public void SubmitRequest()
    {
        if (Mode != AccountMode.Client || !IsSignedIn || !HasRequestDraft
            || DraftError != null || RegionCode.Length == 0 || SettlementCode.Length == 0
            || ExactAddress.Length == 0)
        {
            return;
        }

        var request = new RepairRequest(
            _nextRequestId++, CategoryId, Category, Category, Description.Trim(),
            string.Join(", ", Symptoms), Budget.Trim(),
            new WorkArea(RegionCode, SettlementCode, CityDistrictCode, District.Trim()),
            DraftEquipment, PhotoPath);
        var order = new RepairOrder(request, ExactAddress);
        _orders.Add(request.Id, order);
        if (SaveAddress)
        {
            SavedAddress = $"{request.Area.DisplayName}, {order.ExactAddress}";
        }

        CategoryId = "";
        OtherCategory = "";
        Symptoms.Clear();
        Description = "";
        DraftEquipment = null;
        PhotoPath = "";
        RegionCode = "";
        SettlementCode = "";
        CityDistrictCode = "";
        District = "";
        Street = "";
        Building = "";
        Budget = "1500";
        SaveAddress = false;
        HasRequestDraft = false;

        OpenOrder(order, "Submitted");
    }

    public bool AddChatMessage(RepairOrder order)
    {
        string text = order.GetMessageDraft(Mode).Trim();
        if (CurrentOrder != order || !CanChat(order) || text.Length == 0)
        {
            return false;
        }

        order.Messages.Add(new(text, DateTime.Now.ToString("HH:mm"), Mode == AccountMode.Client));
        order.SetMessageDraft(Mode, "");
        return true;
    }

    public static string OrderStatusLabel(OrderStatus status) =>
        status switch
        {
            OrderStatus.Submitted => "Нова",
            OrderStatus.HasResponses => "Є пропозиції",
            OrderStatus.MasterSelected => "Майстра обрано",
            OrderStatus.VisitRecorded => "Час записано",
            OrderStatus.Completed => "Завершено",
            OrderStatus.Cancelled => "Скасовано",
            _ => "Чернетка",
        };
    public void SaveMasterProposal(RepairRequest request, bool sent)
    {
        string reply = MasterReply.Trim();
        string price = MasterPrice.Trim();
        var order = FindOrder(request.Id);
        if (Mode != AccountMode.Master || MasterProposals.GetValueOrDefault(request.Id).Sent
            || (order != null && order.Status is not
                (OrderStatus.Submitted or OrderStatus.HasResponses)))
        {
            return;
        }

        MasterProposals[request.Id] = (reply, price, sent);
        if (sent && order != null)
        {
            string name = string.IsNullOrWhiteSpace(ProfileName) ? "Майстер із цієї сесії"
                : ProfileName.Trim();
            var offer = new MasterOffer(LocalMasterId, name, request.Category, 0,
                reply, price + " грн", "", "", "");
            order.Offers.Add(offer);
            order.Status = OrderStatus.HasResponses;
        }
    }

    public void RecordVisit(RepairOrder order)
    {
        if (CurrentOrder != order || Mode != AccountMode.Client || !CanChat(order))
        {
            return;
        }

        order.RecordedVisitText = $"{order.VisitDate:d} · {order.VisitWindow.Trim()}";
        order.Status = OrderStatus.VisitRecorded;
        CurrentScreen = "Orders";
    }

    public string MasterReply { get; set; } = "Можу приїхати завтра.";
    public string MasterPrice { get; set; } = "1200";
    public string MasterPortfolioPhotoPath { get; set; } = "";
    public bool IsMasterProfileActive { get; set; }
    public HashSet<string> MasterCategories { get; } = new(StringComparer.Ordinal);
    public string MasterOtherCategory { get; set; } = "";
    public WorkArea MasterArea { get; set; } = WorkArea.Empty;
    public WorkArea MasterFilterArea { get; set; } = WorkArea.Empty;
    public bool FilterMasterServices { get; set; }
    public RepairRequest? ViewedRequest { get; set; }
    public Dictionary<int, (string Reply, string Price, bool Sent)> MasterProposals { get; } = new();
    public string MasterAbout { get; set; } = "";
    public string EquipmentName { get; set; } = "";
    public string EquipmentType { get; set; } = "";
    public string EquipmentModel { get; set; } = "";
    public string EquipmentNotes { get; set; } = "";
    public List<EquipmentRecord> Equipment { get; } =
        new() { new("Змішувач у ванній", "Сантехніка", "Grohe") };
    public void SetMode(AccountMode mode)
    {
        Mode = mode;
        CurrentScreen =
            mode == AccountMode.Client ? "Home"
            : IsMasterProfileActive ? "AvailableRequests"
            : "MasterActivation";
    }

    public void ChooseMaster(RepairOrder order, MasterOffer selectedMaster)
    {
        if (CurrentOrder != order || Mode != AccountMode.Client
            || order.Status != OrderStatus.HasResponses || !order.Offers.Contains(selectedMaster))
        {
            return;
        }

        order.SelectedMaster = selectedMaster;
        order.Status = OrderStatus.MasterSelected;
        if (!order.IsLocalMasterSelected)
        {
            order.Messages.Add(new(
                "Добрий день! Можу приїхати завтра о 17:00.", "10:24", false));
        }

        CurrentScreen = "Chat";
    }

    public void NotifyAll() => OnPropertyChanged("");

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new(propertyName));
}
