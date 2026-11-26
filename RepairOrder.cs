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
        new(PrototypeViewModel.DemoMasterId, "Олександр Коваль", request.Category, 8,
            "Можу взятися за роботу й уточнити деталі.", "1 200 грн", "master_oleksandr.jpg"),
    ];
    public bool MasterReportedCompletion { get; set; }
    public ServiceInvitation? Invitation { get; set; }
    public OrderStatus? StatusBeforeCompletion { get; set; }

    public List<ChatMessage> Messages { get; } = new();
    public DateTime VisitDate { get; set; } = DateTime.Today.AddDays(2);
    public string VisitWindow { get; set; } = "Після 17:00";
    public string RecordedVisitText { get; set; } = "";
    public MasterReview? Review { get; set; }

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
