namespace BudApka;

internal sealed record ServiceCard(int Id, string CategoryId, string Category, string Description,
    WorkArea Area)
{
    public bool Matches(RepairRequest request) =>
        Matches(request.CategoryId, request.Category, request.Area);

    public bool Matches(string categoryId, string category, WorkArea area) =>
        CategoryId == categoryId
        && (CategoryId != RepairCatalog.Other
            || Category.Equals(category, StringComparison.CurrentCultureIgnoreCase))
        && Area.Includes(area);
}

internal sealed record ServiceInvitation(ServiceCard Service, bool Declined = false);

internal sealed partial class PrototypeViewModel
{
    private readonly List<ServiceCard> _serviceCards = new();
    private int _nextServiceId = 1;

    public IEnumerable<ServiceCard> ServiceCards => _serviceCards.AsEnumerable().Reverse();
    public int? DraftServiceId { get; set; }

    public ServiceCard? GetMatchingDraftService() =>
        IsMasterProfileActive && DraftServiceId is int id
            ? _serviceCards.Find(card => card.Id == id
                && card.Matches(CategoryId, Category, DraftArea))
            : null;

    public bool SaveServiceCard(ServiceCard card)
    {
        if (Mode != AccountMode.Master || !IsMasterProfileActive
            || RepairCatalog.Find(card.CategoryId) == null
            || string.IsNullOrWhiteSpace(card.Category)
            || string.IsNullOrWhiteSpace(card.Area.RegionCode))
        {
            return false;
        }

        if (card.Id == 0)
        {
            _serviceCards.Add(card with { Id = _nextServiceId++ });
            return true;
        }

        int index = _serviceCards.FindIndex(item => item.Id == card.Id);
        if (index < 0)
        {
            return false;
        }

        _serviceCards[index] = card;
        return true;
    }

    public IEnumerable<ServiceCard> MissingProfileServiceCards() =>
        RepairCatalog.Categories
            .Where(category => MasterCategories.Contains(category.Id)
                && _serviceCards.All(card => card.CategoryId != category.Id))
            .Select(category => new ServiceCard(0, category.Id,
                category.Id == RepairCatalog.Other ? MasterOtherCategory.Trim() : category.Name,
                "", MasterArea))
            .Where(card => card.Category.Length > 0);

    public bool RemoveServiceCard(ServiceCard card) =>
        Mode == AccountMode.Master && _serviceCards.Remove(card);

    public IEnumerable<ServiceCard> MatchingServices(RepairOrder order) =>
        IsMasterProfileActive && IsOpenForResponses(order)
            ? ServiceCards.Where(card => card.Matches(order.Request)) : [];

    public bool CanInviteForService(RepairOrder order, ServiceCard card) =>
        Mode == AccountMode.Client && order.Invitation == null
        && order.Offers.All(offer => offer.Id != LocalMasterId)
        && MatchingServices(order).Contains(card);

    public bool InviteForService(RepairOrder order, ServiceCard card)
    {
        if (!CanInviteForService(order, card))
        {
            return false;
        }

        order.Invitation = new ServiceInvitation(card);
        _notifications.Add(new(order.Request.Id, AccountMode.Master,
            "Запрошення на послугу", order.Request.Category) { OpensServiceInvitations = true });
        return true;
    }

    public static bool HasPendingInvitation(RepairOrder order) =>
        IsOpenForResponses(order) && order.Invitation is { Declined: false };

    private static bool IsOpenForResponses(RepairOrder order) =>
        order.Status is OrderStatus.Submitted or OrderStatus.HasResponses;

    public bool RespondToServiceInvitation(RepairOrder order, bool accept)
    {
        if (Mode != AccountMode.Master || !IsMasterProfileActive
            || !HasPendingInvitation(order) || order.Invitation is not { } invitation)
        {
            return false;
        }

        if (accept)
        {
            var service = invitation.Service;
            var offer = new MasterOffer(LocalMasterId, ProfileName.Trim(), service.Category,
                0, service.Description, "", "");
            order.Offers.Add(offer);
            order.SelectedMaster = offer;
            order.Status = OrderStatus.MasterSelected;
        }
        else
        {
            order.Invitation = invitation with { Declined = true };
        }

        _notifications.Add(new(order.Request.Id, AccountMode.Client,
            accept ? "Майстер прийняв запрошення" : "Майстер відхилив запрошення",
            order.Request.Category) { OpensOrders = true });
        return true;
    }
}
