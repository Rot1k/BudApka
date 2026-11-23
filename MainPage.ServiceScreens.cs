namespace BudApka;

public partial class MainPage
{
    ServiceCard? _editingService;

    View MyServices()
    {
        if (_viewModel.Mode != AccountMode.Master || !_viewModel.IsMasterProfileActive)
        {
            return Home();
        }

        var page = Root("Мої картки послуг");
        page.Add(CreateButton("Додати послугу", true, () => OpenServiceEditor(null), "plus"));
        var profileCards = _viewModel.MissingProfileServiceCards().ToArray();
        if (profileCards.Length > 0)
        {
            page.Add(CreateAsyncButton("Створити картки з профілю", false, async () =>
            {
                string categories = string.Join(", ", profileCards.Select(card => card.Category));
                if (!await DisplayAlertAsync("Опублікувати картки з профілю?",
                    $"Послуги: {categories}. Зона роботи буде взята з профілю.",
                    "Опублікувати", "Скасувати"))
                {
                    return;
                }

                foreach (var card in _viewModel.MissingProfileServiceCards().ToArray())
                {
                    _viewModel.SaveServiceCard(card);
                }
                _viewModel.NotifyAll();
            }, "tools"));
        }
        foreach (var service in _viewModel.ServiceCards)
        {
            var card = ServiceSummary(service);
            card.Add(CreateButton("Редагувати", false, () => OpenServiceEditor(service)));
            card.Add(CreateAsyncButton("Зняти з публікації", false, async () =>
            {
                if (await DisplayAlertAsync("Зняти картку з публікації?",
                    "Картка зникне з каталогу та рекомендацій.", "Зняти", "Залишити")
                    && _viewModel.RemoveServiceCard(service))
                {
                    _viewModel.NotifyAll();
                }
            }));
            page.Add(Card(card));
        }
        return page;
    }

    void OpenServiceEditor(ServiceCard? service)
    {
        _editingService = service;
        _viewModel.CurrentScreen = "ServiceEditor";
    }

    View ServiceEditor()
    {
        if (_viewModel.Mode != AccountMode.Master || !_viewModel.IsMasterProfileActive)
        {
            return Home();
        }

        var existing = _editingService;
        var page = Root(existing == null ? "Нова картка послуги" : "Редагувати картку послуги",
            "Зміни в картці діють лише для цієї послуги.");
        string initialCategory = existing?.CategoryId
            ?? RepairCatalog.Categories.FirstOrDefault(
                item => _viewModel.MasterCategories.Contains(item.Id))?.Id ?? "";
        string customCategory = existing?.CategoryId == RepairCatalog.Other ? existing.Category
            : _viewModel.MasterOtherCategory;
        string description = existing?.Description ?? "";
        WorkArea area = existing?.Area ?? _viewModel.MasterArea;

        var category = new Picker
        {
            Title = "Оберіть категорію послуги",
            ItemsSource = RepairCatalog.Categories.ToArray(),
            ItemDisplayBinding = new Binding(nameof(RepairCategory.Name)),
            SelectedItem = RepairCatalog.Find(initialCategory),
        };
        var categoryError = ValidationMessage();
        categoryError.Text = "Оберіть категорію послуги.";
        View customField = LabeledField("Назва послуги", customCategory, "Наприклад, ремонт ролет",
            null, value => customCategory = value,
            validate: value => category.SelectedItem is RepairCategory { Id: RepairCatalog.Other }
                ? Required(value, "Вкажіть назву послуги.") : null,
            maxLength: 80);
        customField.IsVisible = initialCategory == RepairCatalog.Other;
        category.SelectedIndexChanged += (_, _) =>
        {
            categoryError.IsVisible = false;
            customField.IsVisible = category.SelectedItem is RepairCategory { Id: RepairCatalog.Other };
        };
        page.Add(Body("Категорія", 14));
        page.Add(category);
        page.Add(categoryError);
        page.Add(customField);

        page.Add(LabeledField("Опис послуги · необов’язково", description,
            "Які роботи можете виконати?", null, value => description = value, maxLength: 280));
        page.Add(Body("Зона роботи", 16));
        var controls = AddAreaFields(page, area, value => area = value, requireSettlement: false);
        page.Add(CreateButton(existing == null ? "Опублікувати послугу" : "Зберегти зміни", true, () =>
        {
            if (category.SelectedItem is not RepairCategory selected)
            {
                categoryError.IsVisible = true;
                ScrollToValidation(categoryError);
                return;
            }
            if (!ValidateFields() || !controls.Validate())
            {
                return;
            }

            string name = selected.Id == RepairCatalog.Other ? customCategory.Trim() : selected.Name;
            var card = new ServiceCard(existing?.Id ?? 0, selected.Id, name, description.Trim(),
                area);
            if (_viewModel.SaveServiceCard(card))
            {
                GoBack();
            }
        }));
        return page;
    }

    View ServiceCatalog()
    {
        var page = Root("Знайти майстра");
        var categories = new[] { "Усі категорії" }
            .Concat(RepairCatalog.Categories.Select(item => item.Name)).ToArray();
        var filter = new Picker { ItemsSource = categories, SelectedIndex = 0 };
        var results = new VerticalStackLayout { Spacing = 12 };
        page.Add(filter);
        page.Add(results);
        filter.SelectedIndexChanged += (_, _) => RefreshResults();
        RefreshResults();
        return page;

        void RefreshResults()
        {
            results.Children.Clear();
            string? categoryId = filter.SelectedIndex > 0
                ? RepairCatalog.Categories[filter.SelectedIndex - 1].Id : null;
            foreach (var service in _viewModel.ServiceCards
                .Where(item => categoryId == null || item.CategoryId == categoryId))
            {
                var card = ServiceSummary(service);
                card.Add(CreateAsyncButton("Запросити майстра", true, async () =>
                {
                    if (_viewModel.HasRequestDraft)
                    {
                        await ReplaceDraftAsync(startNew: true, service: service);
                    }
                    else
                    {
                        OpenNewRequest(service);
                    }
                }));
                results.Add(Card(card));
            }

            if (results.Children.Count == 0)
            {
                results.Add(Body("Опублікованих послуг поки немає.", 14, true));
            }
        }
    }

    void OpenNewRequest(ServiceCard? service)
    {
        if (service != null)
        {
            _viewModel.SelectCategory(service.CategoryId);
            if (service.CategoryId == RepairCatalog.Other)
            {
                _viewModel.OtherCategory = service.Category;
            }
            _viewModel.DraftServiceId = service.Id;
        }
        _viewModel.CurrentScreen = "NewRequest";
    }

    VerticalStackLayout ServiceSummary(ServiceCard service)
    {
        var details = new VerticalStackLayout { Spacing = 8 };
        details.Add(Body(service.Category, 18));
        details.Add(Body(_viewModel.ProfileName, 16));
        details.Add(WithIcon("pin", Body(service.Area.DisplayName, 14)));
        if (service.Description.Length > 0)
        {
            details.Add(Body(service.Description, 14));
        }
        details.Add(MasterStatsView(PrototypeViewModel.LocalMasterId));
        return details;
    }

    void AddServiceRecommendations(VerticalStackLayout page, RepairOrder order)
    {
        page.Add(Body("Рекомендовані майстри", 18));
        if (order.ServiceSelectionUnavailable)
        {
            page.Add(Body("Заявку опубліковано. Вибрана послуга вже недоступна "
                + "або не відповідає категорії чи адресі.", 14));
        }

        var services = _viewModel.MatchingServices(order).ToArray();
        if (services.Length == 0 && order.Invitation == null)
        {
            page.Add(Body("Поки немає послуг для цієї категорії та зони роботи.", 14, true));
        }
        foreach (var service in services)
        {
            var card = ServiceSummary(service);
            if (_viewModel.CanInviteForService(order, service))
            {
                card.Add(CreateAsyncButton("Запросити майстра", true, async () =>
                {
                    if (await DisplayAlertAsync("Запросити майстра?",
                        "Майстер побачить точну адресу після прийняття запрошення.",
                        "Запросити", "Повернутися") && _viewModel.InviteForService(order, service))
                    {
                        _viewModel.NotifyAll();
                    }
                }));
            }
            page.Add(Card(card));
        }
        AddInvitationStatus(page, order);
    }

    void AddInvitationStatus(VerticalStackLayout page, RepairOrder order)
    {
        if (order.Invitation?.Declined == true)
        {
            page.Add(Body("Майстер відхилив запрошення.", 14));
        }
        else if (PrototypeViewModel.HasPendingInvitation(order))
        {
            page.Add(Body("Запрошення надіслано. Очікуємо відповіді майстра.", 14));
        }
    }

    View ServiceInvitations()
    {
        var page = Root("Запрошення на роботи");
        if (_viewModel.Mode != AccountMode.Master)
        {
            return Home();
        }

        var orders = _viewModel.Orders.Where(PrototypeViewModel.HasPendingInvitation).ToArray();
        if (orders.Length == 0)
        {
            page.Add(Body("Нових запрошень поки немає."));
        }
        foreach (var order in orders)
        {
            var request = order.Request;
            var card = new VerticalStackLayout { Spacing = 8 };
            card.Add(Body($"№{request.Id} · {request.Category}", 18));
            card.Add(Body(request.Area.DisplayName, 14));
            card.Add(Body(request.Description, 14));
            if (request.Symptoms.Length > 0)
            {
                card.Add(Body(request.Symptoms, 14, true));
            }
            card.Add(CreateButton("Прийняти", true, () =>
            {
                if (_viewModel.RespondToServiceInvitation(order, accept: true))
                {
                    _viewModel.OpenOrder(order, "Chat");
                }
            }, "check"));
            card.Add(CreateButton("Відхилити", false, () =>
            {
                if (_viewModel.RespondToServiceInvitation(order, accept: false))
                {
                    _viewModel.NotifyAll();
                }
            }));
            page.Add(Card(card));
        }
        return page;
    }
}
