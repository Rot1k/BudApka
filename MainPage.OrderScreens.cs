using Microsoft.Maui.Controls.Shapes;

namespace BudApka;

public partial class MainPage
{
    View Visit()
    {
        if (_viewModel.Mode != AccountMode.Client
            || _viewModel.CurrentOrder is not { } order || !_viewModel.CanChat(order))
        {
            return Orders();
        }


        var r = Root(
            "Запишіть обговорений час",
            "Це зручна нотатка після чату, не формальний контракт."
        );
        r.Add(Body("Дата", 14));
        var datePicker = new DatePicker
        {
            Date = order.VisitDate,
            MinimumDate = DateTime.Today,
            FontFamily = "Inter",
            FontSize = 16,
            TextColor = ThemeColor("#202823", "#F4F7FB"),
            BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
        };
        datePicker.DateSelected += (_, e) => order.VisitDate = e.NewDate ?? DateTime.Today;
        r.Add(
            new Border
            {
                BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
                Stroke = ThemeColor("#727D72", "#91A1B7"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(12, 0),
                HeightRequest = 52,
                Content = datePicker,
            }
        );
        r.Add(
            LabeledField(
                "Зручний проміжок",
                order.VisitWindow,
                "Після 17:00",
                "Довільний текст — без фіксованих слотів.",
                v => order.VisitWindow = v,
                validate: v => Required(v, "Вкажіть погоджений час, наприклад «після 17:00».")
            )
        );
        var details = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Body("ДОСТУПНО ЛИШЕ ОБРАНОМУ МАЙСТРУ", 14, true),
                new Label
                {
                    Text = order.Request.Category,
                    FontFamily = "Manrope",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ThemeColor("#202823", "#F4F7FB"),
                },
                Body(order.SelectedMaster?.Name ?? "Олександр Коваль"),
                Body(order.ExactAddress),
            },
        };
        if (order.SelectedMaster is { Price.Length: > 0 } master)
        {
            details.Add(Body("Орієнтир: " + master.Price));
        }
        r.Add(Card(details));
        r.Add(CreateButton("Зберегти деталі візиту", true, () =>
            {
                if (ValidateFields())
                {
                    _viewModel.RecordVisit(order);
                }
            }));
        r.Add(CreateButton("Повернутися до чату", false, () => _viewModel.CurrentScreen = "Chat"));
        return r;
    }

    View Completion()
    {
        if (_viewModel.Mode != AccountMode.Client || _viewModel.CurrentOrder is not { } order
            || order.Status != OrderStatus.Completed)
        {
            return Orders();
        }


        var r = Root(
            "Як усе минуло?",
            "Роботу позначено завершеною. За бажанням залиште оцінку та відгук."
        );
        r.Add(
            new Border
            {
                BackgroundColor = Color.FromArgb(IsDarkTheme ? "#244D38" : "#EAF4ED"),
                Stroke = ThemeColor("#E3E7DF", "#344151"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = 16,
                Content = new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        StatusBadge(OrderStatus.Completed),
                        new Label
                        {
                            Text = order.Request.Category,
                            FontFamily = "Manrope",
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = ThemeColor("#202823", "#F4F7FB"),
                        },
                        Body(
                            (order.SelectedMaster?.Name ?? "Олександр Коваль")
                                + $" · заявка №{order.Request.Id}"
                        ),
                    },
                },
            }
        );
        string ratingDraft = order.Rating.ToString();
        r.Add(LabeledField(
            "Оцінка", ratingDraft, "5 із 5", "Оберіть оцінку від 1 до 5.",
            value => ratingDraft = value,
            Keyboard.Numeric,
            validate: value => int.TryParse(value, out int rating) && rating is >= 1 and <= 5
                ? null : "Вкажіть оцінку від 1 до 5.",
            allowCharacter: InputRules.Digit, maxLength: 1));
        r.Add(Body("Відгук · необов’язково", 14));
        var review = new Editor
        {
            Text = order.ReviewText,
            Placeholder = "Напишіть кілька слів про роботу",
            FontFamily = "Inter",
            FontSize = 16,
            TextColor = ThemeColor("#202823", "#F4F7FB"),
            PlaceholderColor = ThemeColor("#566257", "#BFCADA"),
            BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
            HeightRequest = 112,
        };
        review.TextChanged += (_, e) => order.ReviewText = e.NewTextValue;
        r.Add(review);
        if (order.StatusBeforeCompletion is { } previous)
        {
            r.Add(CreateButton("Повернути попередній статус", false, () =>
            {
                order.Status = previous;
                order.StatusBeforeCompletion = null;
                order.ReviewText = "";
                _viewModel.CurrentScreen = "Orders";
            }));
        }
        r.Add(CreateButton("Зберегти відгук", true, () =>
        {
            if (!ValidateFields() || !int.TryParse(ratingDraft, out int rating))
            {
                return;
            }

            order.Rating = rating;
            order.ReviewText = order.ReviewText.Trim();
            order.HasReview = true;
            _viewModel.CurrentScreen = "Orders";
        }));
        r.Add(CreateButton("Пропустити відгук", false, () =>
        {
            order.ReviewText = "";
            order.Rating = 5;
            _viewModel.CurrentScreen = "Orders";
        }));
        return r;
    }

    View Orders()
    {
        if (_viewModel.Mode == AccountMode.Master)
        {
            return MasterJobs();
        }
        var r = Root("Мої замовлення", "Поточний статус і наступна дія — в одному місці.");
        var filter = _clientOrdersFilter;
        AddOrderFilters(r, filter);
        var orders = _viewModel.Orders
            .Where(order => filter.Matches(order.Request.CategoryId, order.Status))
            .ToArray();
        foreach (var order in orders)
        {
            r.Add(OrderCard(order, AccountMode.Client));
        }

        if (!AddDemoHistory(r, filter) && orders.Length == 0)
        {
            AddOrderEmptyState(r, filter);
        }

        r.Add(CreateButton(
            _viewModel.HasRequestDraft ? "Продовжити заявку" : "Створити заявку",
            true, () => _viewModel.CurrentScreen = "NewRequest", "plus"));
        return r;
    }

    async Task ChangeOrderStatusAsync(RepairOrder order, OrderStatus next)
    {
        OrderStatus previous = order.Status;
        if (previous is OrderStatus.Completed or OrderStatus.Cancelled
            || (next == OrderStatus.Completed && !CanConfirmCompletion(order)))
        {
            return;
        }


        bool completing = next == OrderStatus.Completed;
        bool confirmed = await DisplayAlertAsync(
            completing ? "Завершити замовлення?" : "Скасувати замовлення?",
            completing ? "Замовлення буде позначено завершеним."
                : "Замовлення буде скасовано, а активний чат закриється.",
            completing ? "Завершити" : "Скасувати замовлення",
            "Повернутися"
        );

        if (!confirmed || order.Status != previous || _viewModel.Mode != AccountMode.Client
            || _viewModel.CurrentScreen != "Orders")
        {
            return;
        }

        order.Status = next;
        SemanticScreenReader.Default.Announce(PrototypeViewModel.OrderStatusLabel(next));
        if (completing)
        {
            order.StatusBeforeCompletion = previous;
            _viewModel.OpenOrder(order, "Completion");
        }
        else
        {
            _viewModel.NotifyAll();
        }
    }

    static bool CanConfirmCompletion(RepairOrder order) =>
        order.Status == OrderStatus.VisitRecorded
        || (order.Status == OrderStatus.MasterSelected && order.MasterReportedCompletion);

    async Task ReportWorkCompletedAsync(RepairOrder order)
    {
        bool confirmed = await DisplayAlertAsync("Роботу виконано?",
            "Клієнт отримає сповіщення й зможе підтвердити завершення.",
            "Повідомити клієнта", "Повернутися");
        if (confirmed && _viewModel.CurrentScreen == "Orders"
            && _viewModel.ReportWorkCompleted(order))
        {
            _viewModel.NotifyAll();
        }
    }

    View MasterJobs()
    {
        var r = Root("Мої роботи", "Замовлення, у яких клієнт обрав вас.");
        var filter = _masterOrdersFilter;
        AddOrderFilters(r, filter);
        var orders = _viewModel.Orders
            .Where(order => order.IsLocalMasterSelected
                && filter.Matches(order.Request.CategoryId, order.Status))
            .ToArray();
        foreach (var order in orders)
        {
            r.Add(OrderCard(order, AccountMode.Master));
        }

        if (!AddDemoHistory(r, filter) && orders.Length == 0)
        {
            AddOrderEmptyState(r, filter);
        }

        if (orders.Length == 0)
        {
            r.Add(CreateButton("До доступних заявок", true, () =>
                _viewModel.CurrentScreen = "AvailableRequests"));
        }

        return r;
    }

    View OrderCard(RepairOrder order, AccountMode mode)
    {
        var request = order.Request;
        var content = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                StatusBadge(order.Status),
                Body($"№{request.Id} · {request.Category}", 18, true),
                Body(request.Area.DisplayName),
                Body(EquipmentSummary(request.Equipment), 14),
            },
        };
#if DEBUG
        if (request.Title.StartsWith("Тест · ", StringComparison.Ordinal))
        {
            content.Add(Body(request.Title, 14, true));
        }
#endif
        bool client = mode == AccountMode.Client;
        if (client)
        {
            content.Add(Body(order.SelectedMaster?.Name ?? "Майстра ще не обрано"));
            AddInvitationStatus(content, order);
        }
        else if (_viewModel.CanChat(order))
        {
            content.Add(Body("Точна адреса: " + order.ExactAddress));
        }

        if (order.RecordedVisitText.Length > 0)
        {
            content.Add(Body(order.RecordedVisitText, 14, true));
        }

        if (order.Status == OrderStatus.Completed && order.HasReview)
        {
            content.Add(Body($"Оцінка: {order.Rating} із 5", 14, true));
            if (order.ReviewText.Length > 0)
            {
                content.Add(Body(order.ReviewText, 14));
            }
        }

        if (client && order.Status == OrderStatus.HasResponses)
        {
            content.Add(CreateButton("Переглянути відповіді", true, () =>
                _viewModel.OpenOrder(order, "Responses")));
        }

        if (_viewModel.CanChat(order))
        {
            content.Add(CreateButton("Відкрити чат", !client, () =>
                _viewModel.OpenOrder(order, "Chat"), "chat"));
        }

        if (order.Status is OrderStatus.MasterSelected or OrderStatus.VisitRecorded)
        {
            if (order.MasterReportedCompletion)
            {
                content.Add(Body(client ? "Майстер повідомив, що роботу виконано."
                    : "Ви позначили роботу виконаною. Очікуємо підтвердження клієнта.", 14));
            }
            else if (!client && order.IsLocalMasterSelected)
            {
                content.Add(CreateAsyncButton("Позначити роботу виконаною", true,
                    () => ReportWorkCompletedAsync(order), "check"));
            }
        }

        if (client && CanConfirmCompletion(order))
        {
            content.Add(CreateAsyncButton("Підтвердити завершення", true, () =>
                ChangeOrderStatusAsync(order, OrderStatus.Completed)));
        }

        if (client && order.Status is not (OrderStatus.Completed or OrderStatus.Cancelled))
        {
            content.Add(CreateAsyncButton("Скасувати замовлення", false, () =>
                ChangeOrderStatusAsync(order, OrderStatus.Cancelled)));
        }

        return Card(content);
    }

    bool AddDemoHistory(VerticalStackLayout page, OrderFilter filter)
    {
        bool visible = false;
        foreach (var (category, title, status) in new[]
        {
            ("appliances", "Підключення пральної машини", OrderStatus.Completed),
            ("windows", "Регулювання вікна", OrderStatus.Cancelled),
        })
        {
            if (filter.Matches(category, status))
            {
                var content = new VerticalStackLayout { Spacing = 8 };
                content.Add(StatusBadge(status));
                content.Add(Body(title, 18));
                content.Add(Body(RepairCatalog.Find(category)!.Name, 14, true));
                content.Add(Body("Демонстраційне замовлення", 14, true));
                page.Add(Card(content));
                visible = true;
            }
        }
        return visible;
    }

    void AddOrderEmptyState(VerticalStackLayout page, OrderFilter filter)
    {
        page.Add(Card(Body("За вибраними умовами замовлень немає.")));
        if (filter.Scope == OrderScope.Active && !filter.HasCriteria)
        {
            page.Add(CreateButton("Показати історію", false, () =>
            {
                filter.Scope = OrderScope.History;
                _viewModel.NotifyAll();
            }, "clock"));
        }
    }
}
