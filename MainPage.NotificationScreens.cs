namespace BudApka;

public partial class MainPage
{
    View Notifications()
    {
        var page = Root("Сповіщення");
        var notifications = _viewModel.Notifications.ToArray();
        if (notifications.Length == 0)
        {
            page.Add(Card(Body("Сповіщень поки немає.")));
            return page;
        }

        foreach (var notification in notifications)
        {
            var content = new VerticalStackLayout { Spacing = 8 };
            content.Add(Body(notification.Title, 16, true));
            content.Add(Body($"№{notification.RequestId} · {notification.Category}", 14));
            var order = _viewModel.FindOrder(notification.RequestId);
            if (order != null && _viewModel.Mode == AccountMode.Client
                && order.Status == OrderStatus.HasResponses)
            {
                content.Add(CreateButton("Переглянути відповіді", false, () =>
                    _viewModel.OpenOrder(order, "Responses")));
            }
            else if (order != null && _viewModel.CanRespondInWorkArea(order))
            {
                content.Add(Body(order.Request.Area.DisplayName, 14));
                content.Add(CreateButton("Переглянути заявку", false, () =>
                {
                    if (_viewModel.CanRespondInWorkArea(order))
                    {
                        OpenMasterRequest(order.Request);
                    }
                }, "request"));
            }
            else if (order != null && _viewModel.CanChat(order))
            {
                content.Add(CreateButton("Відкрити чат", false, () =>
                    _viewModel.OpenOrder(order, "Chat"), "chat"));
            }

            page.Add(Card(content));
        }

        return page;
    }
}
