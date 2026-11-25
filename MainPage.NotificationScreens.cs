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

        if (notifications.Any(notification => !notification.IsRead))
        {
            page.Add(CreateButton("Прочитати всі", false, () =>
            {
                _viewModel.MarkNotificationsRead();
                _viewModel.NotifyAll();
            }, "check"));
        }

        foreach (var notification in notifications)
        {
            var content = new VerticalStackLayout { Spacing = 8 };
            content.Add(Body(notification.Title, 16, true));
            content.Add(Body($"№{notification.RequestId} · {notification.Category}", 14));
            content.Add(Body(notification.IsRead ? "Прочитано" : "Непрочитано", 14,
                !notification.IsRead));
            if (!notification.IsRead)
            {
                content.Add(CreateButton("Позначити прочитаним", false, () =>
                {
                    notification.IsRead = true;
                    _viewModel.NotifyAll();
                }, "check"));
            }

            var order = _viewModel.FindOrder(notification.RequestId);
            if (order != null && notification.OpensServiceInvitations)
            {
                content.Add(CreateButton("Переглянути запрошення", false, () =>
                    OpenNotification(() => _viewModel.CurrentScreen = "ServiceInvitations"),
                    "bell"));
            }
            else if (order != null && notification.OpensOrders)
            {
                content.Add(CreateButton("Мої замовлення", false, () =>
                    OpenNotification(() => _viewModel.CurrentScreen = "Orders"), "request"));
            }
            else if (order != null && _viewModel.Mode == AccountMode.Client
                && order.Status == OrderStatus.HasResponses)
            {
                content.Add(CreateButton("Переглянути відповіді", false, () =>
                    OpenNotification(() => _viewModel.OpenOrder(order, "Responses"))));
            }
            else if (order != null && _viewModel.CanRespondInWorkArea(order))
            {
                content.Add(Body(order.Request.Area.DisplayName, 14));
                content.Add(CreateButton("Переглянути заявку", false, () =>
                {
                    if (_viewModel.CanRespondInWorkArea(order))
                    {
                        OpenNotification(() => OpenMasterRequest(order.Request));
                    }
                }, "request"));
            }
            else if (order != null && _viewModel.CanChat(order))
            {
                content.Add(CreateButton("Відкрити чат", false, () =>
                    OpenNotification(() => _viewModel.OpenOrder(order, "Chat")), "chat"));
            }

            page.Add(Card(content));

            void OpenNotification(Action open)
            {
                notification.IsRead = true;
                open();
            }
        }

        return page;
    }
}
