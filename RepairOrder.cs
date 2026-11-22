namespace BudApka;

internal sealed class RepairOrder(RepairRequest request, string exactAddress)
{
    private string _clientMessageDraft = "";
    private string _masterMessageDraft = "";

    public RepairRequest Request { get; } = request;
    public string ExactAddress { get; } = exactAddress;
    public OrderStatus Status { get; set; } = OrderStatus.HasResponses;
    public MasterOffer? SelectedMaster { get; set; }
    public MasterOffer? PreviewedMaster { get; set; }
    public bool IsLocalMasterSelected => SelectedMaster?.Id == PrototypeViewModel.LocalMasterId;

    public List<MasterOffer> Offers { get; } =
    [
        new(
            "oleksandr", "Олександр Коваль", request.Category, 8,
            "Можу взятися за роботу й уточнити деталі.", "1 200 грн", "4,9", "12",
            "master_oleksandr.jpg"),
        new(
            "iryna", "Ірина Мельник", request.Category, 6,
            "Вільна завтра після 17:00.", "1 400 грн", "4,9", "12", "master_iryna.jpg"),
    ];

    public List<ChatMessage> Messages { get; } = new();
    public DateTime VisitDate { get; set; } = DateTime.Today.AddDays(2);
    public string VisitWindow { get; set; } = "Після 17:00";
    public string RecordedVisitText { get; set; } = "";
    public bool HasReview { get; set; }
    public int Rating { get; set; } = 5;
    public string ReviewText { get; set; } = "";

    public string GetMessageDraft(AccountMode mode) =>
        mode == AccountMode.Client ? _clientMessageDraft : _masterMessageDraft;

    public void SetMessageDraft(AccountMode mode, string text)
    {
        if (mode == AccountMode.Client)
        {
            _clientMessageDraft = text;
        }
        else
        {
            _masterMessageDraft = text;
        }
    }
}
