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

public sealed class PrototypeViewModel : INotifyPropertyChanged
{
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
    public string Category { get; set; } = "Сантехніка";
    public List<string> Categories { get; } =
        new() { "Сантехніка", "Електрика", "Побутова техніка", "Інше" };
    public List<string> Symptoms { get; } = new() { "Протікає вода" };
    public string Description { get; set; } = "Підтікає змішувач на кухні. Новий уже придбано.";
    public string PhotoPath { get; set; } = "";
    public string RegionCode { get; set; } = "";
    public string SettlementCode { get; set; } = "";
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
    public bool HasOrder { get; private set; }
    public string OrderCategory { get; private set; } = "";
    public string OrderDistrict { get; private set; } = "";
    public string OrderAddress { get; private set; } = "";
    public string SavedAddress { get; private set; } = "";
    public bool CanChat => SelectedMaster != null
        && Status is OrderStatus.MasterSelected or OrderStatus.VisitRecorded;
    public MasterOffer? PreviewedMaster { get; set; }

    public void SubmitRequest()
    {
        if (HasOrder)
        {
            CurrentScreen = "Orders";
            return;
        }

        OrderCategory = Category;
        OrderDistrict = District.Trim();
        OrderAddress = ExactAddress.Trim();
        if (SaveAddress)
        {
            SavedAddress = $"{OrderDistrict}, {OrderAddress}";
        }

        HasOrder = true;
        Status = OrderStatus.HasResponses;
        CurrentScreen = "Submitted";
    }

    public bool AddChatMessage()
    {
        if (!CanChat || string.IsNullOrWhiteSpace(MessageDraft))
        {
            return false;
        }

        Messages.Add(new(MessageDraft.Trim(), DateTime.Now.ToString("HH:mm"),
            Mode == AccountMode.Client));
        MessageDraft = "";
        return true;
    }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public string StatusText =>
        Status switch
        {
            OrderStatus.Submitted => "Заявку надіслано",
            OrderStatus.HasResponses => "Є пропозиції",
            OrderStatus.MasterSelected => "Майстра обрано",
            OrderStatus.VisitRecorded => "Час записано",
            OrderStatus.Completed => "Завершено",
            OrderStatus.Cancelled => "Скасовано",
            _ => "Чернетка",
        };
    public List<MasterOffer> Offers { get; } =
        new()
        {
            new(
                "oleksandr",
                "Олександр Коваль",
                "Сантехнік",
                8,
                "Заміню змішувач і перевірю з'єднання.",
                "1 200 грн",
                "4,9",
                "12",
                "master_oleksandr.jpg"
            ),
            new(
                "iryna",
                "Ірина Мельник",
                "Сантехнік",
                6,
                "Вільна завтра після 17:00.",
                "1 400 грн",
                "4,9",
                "12",
                "master_iryna.jpg"
            ),
        };
    public MasterOffer? SelectedMaster { get; set; }
    public List<ChatMessage> Messages { get; } = new();
    public DateTime VisitDate { get; set; } = DateTime.Today.AddDays(2);
    public string VisitWindow { get; set; } = "Після 17:00";
    public string RecordedVisitText { get; private set; } = "";

    public void RecordVisit()
    {
        RecordedVisitText = $"{VisitDate:d} · {VisitWindow.Trim()}";
        Status = OrderStatus.VisitRecorded;
        CurrentScreen = "Orders";
    }
    public string MessageDraft { get; set; } = "";
    public string MasterReply { get; set; } = "Можу приїхати завтра.";
    public string MasterPrice { get; set; } = "1200";
    public string MasterPortfolioPhotoPath { get; set; } = "";
    public bool IsMasterProfileActive { get; set; }
    public string MasterDistrict { get; set; } = "Центр";
    public string MasterAbout { get; set; } = "";
    public bool ProposalSent { get; set; }
    public string EquipmentName { get; set; } = "";
    public string EquipmentType { get; set; } = "";
    public string EquipmentModel { get; set; } = "";
    public string EquipmentNotes { get; set; } = "";
    public List<EquipmentRecord> Equipment { get; } =
        new() { new("Змішувач у ванній", "Сантехніка", "Grohe") };
    public int Rating { get; set; } = 5;
    public string ReviewText { get; set; } = "";

    public void SetMode(AccountMode mode)
    {
        Mode = mode;
        CurrentScreen =
            mode == AccountMode.Client ? "Home"
            : IsMasterProfileActive ? "AvailableRequests"
            : "MasterActivation";
    }

    public void ChooseMaster(MasterOffer selectedMaster)
    {
        if (!HasOrder || Mode != AccountMode.Client || Status != OrderStatus.HasResponses)
        {
            return;
        }

        SelectedMaster = selectedMaster;
        Status = OrderStatus.MasterSelected;
        Messages.Clear();
        Messages.Add(new("Добрий день! Можу приїхати завтра о 17:00.", "10:24", false));
        CurrentScreen = "Chat";
    }

    public void NotifyAll() => OnPropertyChanged("");

    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new(propertyName));
}
