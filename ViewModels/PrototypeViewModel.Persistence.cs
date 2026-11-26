namespace BudApka;

internal sealed record DraftSnapshot
{
    public string CategoryId { get; init; } = "";
    public string OtherCategory { get; init; } = "";
    public List<string> Symptoms { get; init; } = [];
    public string Description { get; init; } = "";
    public EquipmentRecord? Equipment { get; init; }
    public string PhotoPath { get; init; } = "";
    public string RegionCode { get; init; } = "";
    public string SettlementCode { get; init; } = "";
    public string CityDistrictCode { get; init; } = "";
    public string District { get; init; } = "";
    public string Street { get; init; } = "";
    public string Building { get; init; } = "";
    public string Budget { get; init; } = "";
    public bool SaveAddress { get; init; }
    public bool HasRequestDraft { get; init; }
    public int? ServiceId { get; init; }
}

internal sealed record OrderSnapshot
{
    public required RepairRequest Request { get; init; }
    public string ExactAddress { get; init; } = "";
    public OrderStatus Status { get; init; }
    public List<MasterOffer> Offers { get; init; } = [];
    public string? SelectedMasterId { get; init; }
    public bool MasterReportedCompletion { get; init; }
    public ServiceInvitation? Invitation { get; init; }
    public OrderStatus? StatusBeforeCompletion { get; init; }
    public List<ChatMessage> Messages { get; init; } = [];
    public DateTime VisitDate { get; init; }
    public string VisitWindow { get; init; } = "";
    public string RecordedVisitText { get; init; } = "";
    public MasterReview? Review { get; init; }
    public string ClientMessageDraft { get; init; } = "";
    public string MasterMessageDraft { get; init; } = "";
}

internal sealed record ProposalSnapshot(int RequestId, string Reply, string Price, bool Sent);

internal sealed record PrototypeSnapshot
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;
    public int NextRequestId { get; init; }
    public int NextServiceId { get; init; }
    public bool DebugDataSeeded { get; init; }
    public DraftSnapshot Draft { get; init; } = new();
    public List<OrderSnapshot> Orders { get; init; } = [];
    public List<OrderNotification> Notifications { get; init; } = [];
    public List<ServiceCard> ServiceCards { get; init; } = [];
    public List<ProposalSnapshot> Proposals { get; init; } = [];
    public List<EquipmentRecord> Equipment { get; init; } = [];

    public string ProfileName { get; init; } = "";
    public string Phone { get; init; } = "";
    public bool IsSignedIn { get; init; }
    public string SavedAddress { get; init; } = "";

    public bool IsMasterProfileActive { get; init; }
    public List<string> MasterCategories { get; init; } = [];
    public string MasterOtherCategory { get; init; } = "";
    public WorkArea MasterArea { get; init; } = WorkArea.Empty;
    public string MasterAbout { get; init; } = "";
    public string MasterReply { get; init; } = "";
    public string MasterPrice { get; init; } = "";
    public string MasterPortfolioPhotoPath { get; init; } = "";

    public IEnumerable<string> PhotoPaths() =>
        Orders.Select(order => order.Request.PhotoPath)
            .Append(Draft.PhotoPath)
            .Append(MasterPortfolioPhotoPath)
            .Where(path => path.Length > 0);
}

internal sealed partial class PrototypeViewModel
{
    private const int FirstRequestId = 2053;

    // Copies collections so the snapshot can be written on a background thread.
    public PrototypeSnapshot CreateSnapshot() => new()
    {
        NextRequestId = _nextRequestId,
        NextServiceId = _nextServiceId,
#if DEBUG
        DebugDataSeeded = HasDebugData,
#endif
        Draft = new DraftSnapshot
        {
            CategoryId = CategoryId,
            OtherCategory = OtherCategory,
            Symptoms = Symptoms.ToList(),
            Description = Description,
            Equipment = DraftEquipment,
            PhotoPath = PhotoPath,
            RegionCode = RegionCode,
            SettlementCode = SettlementCode,
            CityDistrictCode = CityDistrictCode,
            District = District,
            Street = Street,
            Building = Building,
            Budget = Budget,
            SaveAddress = SaveAddress,
            HasRequestDraft = HasRequestDraft,
            ServiceId = DraftServiceId,
        },
        Orders = _orders.Values.Select(CreateOrderSnapshot).ToList(),
        Notifications = _notifications.Select(item => item with { }).ToList(),
        ServiceCards = _serviceCards.ToList(),
        Proposals = MasterProposals
            .Select(item => new ProposalSnapshot(item.Key, item.Value.Reply,
                item.Value.Price, item.Value.Sent))
            .ToList(),
        Equipment = Equipment.ToList(),
        ProfileName = ProfileName,
        Phone = Phone,
        IsSignedIn = IsSignedIn,
        SavedAddress = SavedAddress,
        IsMasterProfileActive = IsMasterProfileActive,
        MasterCategories = MasterCategories.ToList(),
        MasterOtherCategory = MasterOtherCategory,
        MasterArea = MasterArea,
        MasterAbout = MasterAbout,
        MasterReply = MasterReply,
        MasterPrice = MasterPrice,
        MasterPortfolioPhotoPath = MasterPortfolioPhotoPath,
    };

    private static OrderSnapshot CreateOrderSnapshot(RepairOrder order) => new()
    {
        Request = order.Request,
        ExactAddress = order.ExactAddress,
        Status = order.Status,
        Offers = order.Offers.ToList(),
        SelectedMasterId = order.SelectedMaster?.Id,
        MasterReportedCompletion = order.MasterReportedCompletion,
        Invitation = order.Invitation,
        StatusBeforeCompletion = order.StatusBeforeCompletion,
        Messages = order.Messages.ToList(),
        VisitDate = order.VisitDate,
        VisitWindow = order.VisitWindow,
        RecordedVisitText = order.RecordedVisitText,
        Review = order.Review,
        ClientMessageDraft = order.GetMessageDraft(AccountMode.Client),
        MasterMessageDraft = order.GetMessageDraft(AccountMode.Master),
    };

    public void RestoreSnapshot(PrototypeSnapshot snapshot)
    {
        RestoreDraft(snapshot.Draft);

        _orders.Clear();
        foreach (var item in snapshot.Orders)
        {
            var order = RestoreOrder(item);
            _orders[order.Request.Id] = order;
        }

        _notifications.Clear();
        _notifications.AddRange(snapshot.Notifications);
        _serviceCards.Clear();
        _serviceCards.AddRange(snapshot.ServiceCards);
        MasterProposals.Clear();
        foreach (var proposal in snapshot.Proposals)
        {
            MasterProposals[proposal.RequestId] =
                (proposal.Reply, proposal.Price, proposal.Sent);
        }
        Equipment.Clear();
        Equipment.AddRange(snapshot.Equipment);

        int lastRequestId = _orders.Keys.DefaultIfEmpty(FirstRequestId - 1).Max();
        _nextRequestId = Math.Max(snapshot.NextRequestId, lastRequestId + 1);
        int lastServiceId = _serviceCards.Select(card => card.Id).DefaultIfEmpty(0).Max();
        _nextServiceId = Math.Max(snapshot.NextServiceId, lastServiceId + 1);
#if DEBUG
        HasDebugData = snapshot.DebugDataSeeded;
#endif

        ProfileName = snapshot.ProfileName;
        Phone = snapshot.Phone;
        IsSignedIn = snapshot.IsSignedIn;
        SavedAddress = snapshot.SavedAddress;
        IsMasterProfileActive = snapshot.IsMasterProfileActive;
        MasterCategories.Clear();
        MasterCategories.UnionWith(snapshot.MasterCategories);
        MasterOtherCategory = snapshot.MasterOtherCategory;
        MasterArea = snapshot.MasterArea;
        MasterAbout = snapshot.MasterAbout;
        MasterReply = snapshot.MasterReply;
        MasterPrice = snapshot.MasterPrice;
        MasterPortfolioPhotoPath = snapshot.MasterPortfolioPhotoPath;
    }

    private void RestoreDraft(DraftSnapshot draft)
    {
        CategoryId = draft.CategoryId;
        OtherCategory = draft.OtherCategory;
        Symptoms.Clear();
        Symptoms.UnionWith(draft.Symptoms);
        Description = draft.Description;
        DraftEquipment = draft.Equipment;
        PhotoPath = draft.PhotoPath;
        RegionCode = draft.RegionCode;
        SettlementCode = draft.SettlementCode;
        CityDistrictCode = draft.CityDistrictCode;
        District = draft.District;
        Street = draft.Street;
        Building = draft.Building;
        Budget = draft.Budget;
        SaveAddress = draft.SaveAddress;
        HasRequestDraft = draft.HasRequestDraft;
        DraftServiceId = draft.ServiceId;
    }

    private static RepairOrder RestoreOrder(OrderSnapshot item)
    {
        var order = new RepairOrder(item.Request, item.ExactAddress)
        {
            Status = item.Status,
            MasterReportedCompletion = item.MasterReportedCompletion,
            Invitation = item.Invitation,
            StatusBeforeCompletion = item.StatusBeforeCompletion,
            VisitDate = item.VisitDate,
            VisitWindow = item.VisitWindow,
            RecordedVisitText = item.RecordedVisitText,
            Review = item.Review,
        };

        // The constructor adds a demo offer; the saved list replaces it.
        order.Offers.Clear();
        order.Offers.AddRange(item.Offers);
        order.SelectedMaster = order.Offers.Find(offer => offer.Id == item.SelectedMasterId);
        order.Messages.AddRange(item.Messages);
        order.SetMessageDraft(AccountMode.Client, item.ClientMessageDraft);
        order.SetMessageDraft(AccountMode.Master, item.MasterMessageDraft);
        return order;
    }
}
