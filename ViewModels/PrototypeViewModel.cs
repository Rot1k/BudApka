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
    string PhotoAsset
);

public sealed record ChatMessage(string Text, string Time, bool FromClient);

public sealed record EquipmentRecord(string Name, string Type, string Model, string Notes = "");

internal sealed record OrderNotification(
    int RequestId, AccountMode Recipient, string Title, string Category)
{
    public bool IsRead { get; set; }
    public bool OpensOrderDetails { get; init; }
}

internal sealed partial class PrototypeViewModel : INotifyPropertyChanged
{
    private readonly Dictionary<int, RepairOrder> _orders = new();
    private readonly List<OrderNotification> _notifications = new();
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
    public string Budget { get; set; } = "";
    public bool SaveAddress { get; set; }
    public string Phone { get; set; } = "";
    public string Code { get; set; } = "";
    public string ProfileName { get; set; } = "";
    public bool HasProfileContactDetails => !string.IsNullOrWhiteSpace(ProfileName)
        && InputRules.ValidateName(ProfileName) == null
        && InputRules.TryNormalizePhone(Phone, out _);
    public bool IsSignedIn { get; set; }
    public bool HasRequestDraft { get; set; }
    public bool HasOrder => _orders.Count > 0;
    public IEnumerable<RepairOrder> Orders =>
        _orders.Values.OrderByDescending(order => order.Request.Id);
    public RepairOrder? CurrentOrder { get; private set; }

    public IEnumerable<OrderNotification> Notifications =>
        _notifications.AsEnumerable().Reverse().Where(notification => notification.Recipient == Mode);

    public int UnreadNotificationCount => Notifications.Count(notification => !notification.IsRead);

    public void MarkNotificationsRead()
    {
        foreach (var notification in Notifications)
        {
            notification.IsRead = true;
        }
    }

    public RepairOrder? FindOrder(int requestId) => _orders.GetValueOrDefault(requestId);

    public void OpenOrder(RepairOrder order, string screen)
    {
        CurrentOrder = order;
        CurrentScreen = screen;
    }

    public string SavedAddress { get; private set; } = "";
    public const string LocalMasterId = "local-master";
    public const string DemoMasterId = "oleksandr";

    public bool CanChat(RepairOrder order) => order.SelectedMaster != null
        && (Mode == AccountMode.Client || order.IsLocalMasterSelected)
        && order.Status is OrderStatus.MasterSelected or OrderStatus.VisitRecorded;

    public bool CanRespondInWorkArea(RepairOrder order) =>
        Mode == AccountMode.Master && IsInMasterWorkArea(order.Request)
        && order.Status is OrderStatus.Submitted or OrderStatus.HasResponses;

    private bool IsInMasterWorkArea(RepairRequest request) =>
        IsMasterProfileActive && MasterArea.RegionCode.Length > 0
        && MasterArea.Includes(request.Area);

    public WorkArea DraftArea =>
        new(RegionCode, SettlementCode, CityDistrictCode, District.Trim());

    public void SubmitRequest(bool withoutInvitation = false)
    {
        if (Mode != AccountMode.Client || !IsSignedIn || !HasRequestDraft
            || DraftError != null || RegionCode.Length == 0 || SettlementCode.Length == 0
            || ExactAddress.Length == 0)
        {
            return;
        }

        var request = new RepairRequest(
            _nextRequestId, CategoryId, Category, Category, Description.Trim(),
            string.Join(", ", Symptoms), Budget.Trim(),
            DraftArea, DraftEquipment, PhotoPath);
        var order = new RepairOrder(request, ExactAddress);
        if (!withoutInvitation && DraftServiceId != null
            && (GetMatchingDraftService() is not { } service || !InviteForService(order, service)))
        {
            return;
        }

        _nextRequestId++;
        _orders.Add(request.Id, order);
        if (IsInMasterWorkArea(request))
        {
            _notifications.Add(new(request.Id, AccountMode.Master,
                "Нова заявка у вашій зоні роботи", request.Category));
        }

        if (SaveAddress)
        {
            SavedAddress = $"{request.Area.DisplayName}, {order.ExactAddress}";
        }

        ClearRequestDraft();
        OpenOrder(order, "Submitted");
    }

    public void ClearRequestDraft()
    {
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
        Budget = "";
        SaveAddress = false;
        HasRequestDraft = false;
        DraftServiceId = null;
    }

    public bool ReportWorkCompleted(RepairOrder order)
    {
        if (Mode != AccountMode.Master || !order.IsLocalMasterSelected
            || order.MasterReportedCompletion
            || order.Status is not (OrderStatus.MasterSelected or OrderStatus.VisitRecorded))
        {
            return false;
        }

        order.MasterReportedCompletion = true;
        _notifications.Add(new(order.Request.Id, AccountMode.Client,
            "Майстер повідомив, що роботу виконано", order.Request.Category)
        {
            OpensOrderDetails = true,
        });
        return true;
    }

    public bool AddChatMessage(RepairOrder order)
    {
        string text = order.GetMessageDraft(Mode).Trim();
        if (CurrentOrder != order || !CanChat(order) || text.Length == 0)
        {
            return false;
        }

        bool fromClient = Mode == AccountMode.Client;
        order.Messages.Add(new(text, DateTime.Now.ToString("HH:mm"), fromClient));
        order.SetMessageDraft(Mode, "");
        if (order.IsLocalMasterSelected)
        {
            var recipient = fromClient ? AccountMode.Master : AccountMode.Client;
            _notifications.Add(new(order.Request.Id, recipient,
                fromClient ? "Нове повідомлення від клієнта" : "Нове повідомлення від майстра",
                order.Request.Category));
        }

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
            || (order != null && (!IsOpenForResponses(order) || HasPendingInvitation(order))))
        {
            return;
        }

        MasterProposals[request.Id] = (reply, price, sent);
        if (sent && order != null)
        {
            string name = string.IsNullOrWhiteSpace(ProfileName) ? "Майстер із цієї сесії"
                : ProfileName.Trim();
            var offer = new MasterOffer(LocalMasterId, name, request.Category, 0,
                reply, price.Length > 0 ? price + " грн" : "", "");
            order.Offers.Add(offer);
            order.Status = OrderStatus.HasResponses;
            _notifications.Add(new(request.Id, AccountMode.Client,
                $"{name} надіслав(-ла) пропозицію", request.Category));
        }
    }

    public string MasterReply { get; set; } = "Можу приїхати завтра.";
    public string MasterPrice { get; set; } = "";
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
        if (order.IsLocalMasterSelected)
        {
            _notifications.Add(new(order.Request.Id, AccountMode.Master,
                "Клієнт обрав вас", order.Request.Category));
        }
        else
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
