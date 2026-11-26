namespace BudApka;

public sealed record MasterReview(string MasterId, int Rating, string Text, DateTime CreatedAt,
    bool IsDemo = false);

internal sealed record MasterReviewItem(int RequestId, string Category, MasterReview Review);

internal sealed record MasterStats(double? AverageRating, int CompletedOrders,
    int DemoRatingCount);

internal sealed partial class PrototypeViewModel
{
    // The built-in demo master starts with aggregate demo ratings that have no texts.
    private const int DemoMasterRatingCount = 12;
    private const int DemoMasterRatingTotal = 59;

    public static bool CanReview(RepairOrder order) =>
        order.Status == OrderStatus.Completed && order.SelectedMaster != null
        && order.Review == null;

    /// <summary>Saves the only review of a completed order. Repeated calls change nothing.</summary>
    public bool SaveReview(RepairOrder order, int rating, string text)
    {
        if (Mode != AccountMode.Client || !CanReview(order) || rating is < 1 or > 5)
        {
            return false;
        }

        order.Review = new MasterReview(order.SelectedMaster!.Id, rating, text.Trim(),
            DateTime.Now);
        order.StatusBeforeCompletion = null;
        if (order.IsLocalMasterSelected)
        {
            _notifications.Add(new(order.Request.Id, AccountMode.Master,
                "Клієнт залишив відгук", order.Request.Category) { OpensOrderDetails = true });
        }

        return true;
    }

    public IEnumerable<MasterReviewItem> GetMasterReviews(string masterId) =>
        _orders.Values
            .Where(order => order.Review?.MasterId == masterId)
            .OrderByDescending(order => order.Review!.CreatedAt)
            .ThenByDescending(order => order.Request.Id)
            .Select(order => new MasterReviewItem(order.Request.Id, order.Request.Category,
                order.Review!));

    public MasterStats GetMasterStats(string masterId)
    {
        int demoRatingCount = masterId == DemoMasterId ? DemoMasterRatingCount : 0;
        int completedCount = demoRatingCount;
        int ratingCount = demoRatingCount;
        double ratingTotal = masterId == DemoMasterId ? DemoMasterRatingTotal : 0;

        completedCount += _orders.Values.Count(order => order.Status == OrderStatus.Completed
            && order.SelectedMaster?.Id == masterId);
        foreach (var item in GetMasterReviews(masterId))
        {
            ratingCount++;
            ratingTotal += item.Review.Rating;
        }

        return new MasterStats(ratingCount == 0 ? null : ratingTotal / ratingCount,
            completedCount, demoRatingCount);
    }
}
