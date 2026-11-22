using Microsoft.Maui.Controls.Shapes;

namespace BudApka;

public partial class MainPage
{
    readonly HashSet<string> _masterCategoryDraft = new(StringComparer.Ordinal);
    WorkArea _masterAreaDraft = WorkArea.Empty;
    string _masterOtherDraft = "";

    void StartMasterProfileEdit()
    {
        _masterCategoryDraft.Clear();
        _masterCategoryDraft.UnionWith(_viewModel.MasterCategories);
        _masterAreaDraft = _viewModel.MasterArea;
        _masterOtherDraft = _viewModel.MasterOtherCategory;
    }

    View ActivateMaster()
    {
        var r = Root(
            "Почніть працювати майстром",
            "Окремий робочий профіль у вашому акаунті. Перевірка документів не проводиться."
        );
        r.Add(Body("Категорії послуг", 14));
        AddRepairCategories(r, _masterCategoryDraft.Contains, id =>
        {
            if (!_masterCategoryDraft.Add(id))
            {
                _masterCategoryDraft.Remove(id);
            }
            _viewModel.NotifyAll();
        });
        if (_masterCategoryDraft.Contains(RepairCatalog.Other))
        {
            r.Add(LabeledField("Інші послуги", _masterOtherDraft,
                "Опишіть, які ще роботи виконуєте", null,
                value => _masterOtherDraft = value,
                validate: value => Required(value, "Опишіть інші послуги."), maxLength: 120));
        }

        r.Add(Body("Зона роботи", 16));
        var controls = AddAreaFields(r, _masterAreaDraft, value => _masterAreaDraft = value,
            requireSettlement: false);
        r.Add(Body("Коротко про себе", 14));
        var about = new Editor
        {
            Text = _viewModel.MasterAbout,
            Placeholder = "Опишіть свій досвід і типові роботи",
            FontFamily = "Inter",
            FontSize = 16,
            TextColor = ThemeColor("#202823", "#F4F7FB"),
            PlaceholderColor = ThemeColor("#566257", "#BFCADA"),
            BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
            HeightRequest = 112,
        };
        about.TextChanged += (_, e) => _viewModel.MasterAbout = e.NewTextValue;
        r.Add(about);
        var portfolio = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label
                {
                    Text = "Портфоліо · додайте приклади робіт",
                    FontFamily = "Inter",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ThemeColor("#202823", "#F4F7FB"),
                },
            },
        };
        if (!string.IsNullOrEmpty(_viewModel.MasterPortfolioPhotoPath))
            portfolio.Add(
                new Image
                {
                    Source = ImageSource.FromFile(_viewModel.MasterPortfolioPhotoPath),
                    HeightRequest = 120,
                    Aspect = Aspect.AspectFill,
                }
            );
        portfolio.Add(CreateAsyncButton("Додати фото", false, async () =>
            {
                var files = await MediaPicker.Default.PickPhotosAsync();
                var file = files.FirstOrDefault();
                if (file != null)
                {
                    _viewModel.MasterPortfolioPhotoPath = file.FullPath;
                    _viewModel.NotifyAll();
                }
            }, "camera"));
        portfolio.Add(Body("До 5 фото · JPG або PNG · до 10 МБ", 14, true));
        r.Add(
            new Border
            {
                BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
                Stroke = ThemeColor("#E3E7DF", "#344151"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = 16,
                Content = portfolio,
            }
        );
        r.Add(CreateAsyncButton(
            _viewModel.IsMasterProfileActive ? "Зберегти умови" : "Активувати профіль майстра",
            true, async () =>
            {
                if (_masterCategoryDraft.Count == 0)
                {
                    await DisplayAlertAsync("Категорії послуг",
                        "Оберіть хоча б одну категорію.", "Готово");
                    return;
                }
                if (!controls.Validate() || !ValidateFields())
                {
                    return;
                }

                if (_viewModel.MasterFilterArea.RegionCode != _masterAreaDraft.RegionCode)
                {
                    _viewModel.MasterFilterArea = WorkArea.Empty;
                }
                _viewModel.MasterArea = _masterAreaDraft;
                _viewModel.MasterCategories.Clear();
                _viewModel.MasterCategories.UnionWith(_masterCategoryDraft);
                _viewModel.MasterOtherCategory = _masterOtherDraft.Trim();
                _viewModel.IsMasterProfileActive = true;
                _viewModel.SetMode(AccountMode.Master);
            }));
        return r;
    }

    static readonly RepairRequest[] DemoRequests =
    [
        new(2051, "plumbing", "Сантехніка", "Заміна сифона",
            "Підтікає під мийкою.", "Підтікає вода", "900",
            new("UA80000000000093317", "UA80000000000093317-city",
                "UA80000000000126643", "Київ · Голосіївський"), null),
        new(2052, "painting", "Фарбування й оздоблення", "Пофарбувати стіни",
            "Потрібно пофарбувати стіни в кімнаті.", "Пофарбувати стіни", "",
            new("UA80000000000093317", "UA80000000000093317-city", "", "Київ"), null),
    ];

    View Available()
    {
        var page = Root("Доступні заявки", "Заявки у вибраному регіоні");
        var area = _viewModel.MasterArea;
        var filter = _viewModel.MasterFilterArea;
        var region = new WorkArea(area.RegionCode, "", "", "");
        var categories = RepairCatalog.Categories.Where(category =>
            _viewModel.MasterCategories.Contains(category.Id)).Select(category =>
            category.Id == RepairCatalog.Other ? _viewModel.MasterOtherCategory : category.Name);
        page.Add(Card(Body("Зона роботи: " + area.DisplayName + Environment.NewLine
            + "Послуги: " + string.Join(", ", categories))));
        page.Add(CreateButton("Змінити умови", false, () =>
            _viewModel.CurrentScreen = "MasterActivation"));
        page.Add(CreateButton("Фільтри", false, () =>
            _viewModel.CurrentScreen = "MasterFilters", "filter"));
        AddFilterSummary(page, _availableOrdersFilter);

        bool filtered = filter.SettlementCode.Length > 0 || _viewModel.FilterMasterServices;
        if (filtered)
        {
            var labels = new List<string>();
            if (filter.SettlementCode.Length > 0)
            {
                labels.Add(filter.DisplayName);
            }
            if (_viewModel.FilterMasterServices)
            {
                labels.Add("Мої послуги");
            }
            page.Add(Body("Фільтри: " + string.Join(" · ", labels), 14));
        }

        var requests = _viewModel.Orders
            .Where(order => order.Status is OrderStatus.Submitted or OrderStatus.HasResponses)
            .Select(order => order.Request)
            .Concat(DemoRequests);

        var results = requests.Where(request => region.Includes(request.Area)
            && (filter.RegionCode.Length == 0 || filter.Includes(request.Area))
            && (!_viewModel.FilterMasterServices
                || _viewModel.MasterCategories.Contains(request.CategoryId))
            && _availableOrdersFilter.Matches(request.CategoryId, AvailableStatus(request)))
            .ToArray();
        foreach (var request in results)
        {
            var card = new VerticalStackLayout { Spacing = 8 };
            card.Add(StatusBadge(AvailableStatus(request)));
            card.Add(Body($"№{request.Id} · {request.Category}", 14, true));
            card.Add(Body(request.Title));
            card.Add(Body(request.Area.DisplayName, 14));
            card.Add(CreateButton("Переглянути заявку", false, () => OpenMasterRequest(request), "request"));
            page.Add(Card(card));
        }
        if (results.Length == 0)
        {
            page.Add(Card(Body("За вибраними умовами заявок поки немає.")));
        }
        if (filtered || _availableOrdersFilter.HasCriteria || results.Length == 0)
        {
            page.Add(CreateButton("Скинути фільтри", false, () =>
            {
                ResetMasterFilters();
                _viewModel.NotifyAll();
            }));
        }
        return page;
    }

    OrderStatus AvailableStatus(RepairRequest request) =>
        _viewModel.FindOrder(request.Id)?.Status
        ?? (_viewModel.MasterProposals.GetValueOrDefault(request.Id).Sent
            ? OrderStatus.HasResponses : OrderStatus.Submitted);

    void ResetMasterFilters()
    {
        _availableOrdersFilter.Reset();
        _viewModel.MasterFilterArea = WorkArea.Empty;
        _viewModel.FilterMasterServices = false;
    }

    View MasterFilters()
    {
        var page = Root("Фільтри", "Уточніть, які заявки бачити в межах регіону.");
        var criteria = new OrderFilter();
        criteria.CopyCriteria(_availableOrdersFilter);
        AddOrderCriteria(page, criteria, ActiveStatuses.Take(2));
        var zone = _viewModel.MasterArea;
        var filter = _viewModel.MasterFilterArea;
        var controls = AddAreaFields(page, filter, value => filter = value,
            requireSettlement: false, fixedRegionCode: zone.RegionCode);
        var onlyServices = new Switch { IsToggled = _viewModel.FilterMasterServices };
        if (zone.SettlementCode.Length > 0)
        {
            page.Add(CreateButton("Застосувати зону з профілю", false, () =>
            {
                _viewModel.MasterFilterArea = zone;
                _availableOrdersFilter.CopyCriteria(criteria);
                _viewModel.FilterMasterServices = onlyServices.IsToggled;
                _viewModel.CurrentScreen = "AvailableRequests";
            }));
        }
        page.Add(Body("Показувати лише мої категорії", 14));
        page.Add(onlyServices);
        page.Add(CreateButton("Застосувати фільтри", true, () =>
        {
            if (controls.Validate())
            {
                _viewModel.MasterFilterArea = filter;
                _availableOrdersFilter.CopyCriteria(criteria);
                _viewModel.FilterMasterServices = onlyServices.IsToggled;
                _viewModel.CurrentScreen = "AvailableRequests";
            }
        }));
        page.Add(CreateButton("Скинути фільтри", false, () =>
        {
            ResetMasterFilters();
            _viewModel.CurrentScreen = "AvailableRequests";
        }));
        return page;
    }

    void OpenMasterRequest(RepairRequest request)
    {
        _viewModel.ViewedRequest = request;
        var proposal = _viewModel.MasterProposals.GetValueOrDefault(request.Id);
        _viewModel.MasterReply = proposal.Reply ?? "";
        _viewModel.MasterPrice = proposal.Price ?? "";
        _viewModel.CurrentScreen = "MasterRequest";
    }

    View MasterRequest()
    {
        if (_viewModel.ViewedRequest is not { } request)
        {
            return Available();
        }

        var page = Root($"Заявка №{request.Id}", request.Title);
        var details = new VerticalStackLayout { Spacing = 8 };
        details.Add(StatusBadge(AvailableStatus(request)));
        details.Add(Body("Категорія: " + request.Category));
        if (request.Symptoms.Length > 0)
        {
            details.Add(Body("Ознаки або роботи: " + request.Symptoms));
        }
        if (request.Description.Length > 0)
        {
            details.Add(Body(request.Description));
        }
        details.Add(Body(request.Area.DisplayName));
        details.Add(WithIcon("budget", Body(
            request.Budget.Length > 0 ? "Бюджет: " + request.Budget + " грн"
                : "Бюджет не вказано", 14)));
        details.Add(Body("Точна адреса доступна після вибору майстра.", 14, true));
        if (request.PhotoPath.Length > 0)
        {
            details.Add(new Image
            {
                Source = ImageSource.FromFile(request.PhotoPath),
                HeightRequest = 120,
                Aspect = Aspect.AspectFit,
            });
        }
        page.Add(Card(details));
        page.Add(Card(Body(EquipmentSummary(request.Equipment), 14)));
        bool sent = _viewModel.MasterProposals.GetValueOrDefault(request.Id).Sent;
        string action = sent ? "Переглянути пропозицію" : "Надіслати пропозицію";
        page.Add(CreateButton(action, true, () =>
            _viewModel.CurrentScreen = sent ? "ProposalSent" : "Proposal"));
        page.Add(CreateButton("Назад до заявок", false, () =>
            _viewModel.CurrentScreen = "AvailableRequests"));
        return page;
    }

    View Proposal()
    {
        if (_viewModel.ViewedRequest is not { } request)
        {
            return Available();
        }
        var r = Root(
            "Ваша відповідь клієнту",
            $"Заявка №{request.Id} · {request.Title}"
        );
        r.Add(
            LabeledField("Повідомлення", _viewModel.MasterReply, "Напишіть клієнту", null, v =>
                _viewModel.MasterReply = v,
                validate: value => Required(value, "Напишіть коротку відповідь клієнту.")
            )
        );
        var price = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label
                {
                    Text = "Орієнтовна ціна, грн",
                    FontFamily = "Inter",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ThemeColor("#202823", "#F4F7FB"),
                },
            },
        };
        price.Add(
            LabeledField(
                "Ціна, грн",
                _viewModel.MasterPrice,
                "1 200",
                "Довідковий текст без розрахунків і платежів.",
                v => _viewModel.MasterPrice = v,
                Keyboard.Numeric,
                validate: ValidateAmount,
                allowCharacter: c => InputRules.Digit(c) || c == ' ',
                maxLength: 14
            )
        );
        r.Add(price);
        r.Add(
            Card(
                new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        new Label
                        {
                            Text = "Що побачить клієнт",
                            FontFamily = "Manrope",
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = ThemeColor("#202823", "#F4F7FB"),
                        },
                        Body(
                            "Ваш профіль, відгуки, відповідь і орієнтовну ціну. "
                                + "Точна адреса відкриється лише після вибору вас."
                        ),
                    },
                }
            )
        );
        r.Add(CreateButton("Надіслати відповідь", true, () =>
            {
                if (ValidateFields())
                {
                    _viewModel.SaveMasterProposal(request, sent: true);
                    _viewModel.CurrentScreen = "ProposalSent";
                }
            }));
        r.Add(CreateButton("Зберегти як чернетку", false, () =>
            {
                _viewModel.SaveMasterProposal(request, sent: false);
                _viewModel.CurrentScreen = "AvailableRequests";
            }));
        return r;
    }

    View ProposalSentScreen()
    {
        if (_viewModel.ViewedRequest is not { } request)
        {
            return Available();
        }

        var proposal = _viewModel.MasterProposals.GetValueOrDefault(request.Id);
        var r = Root(
            "Відповідь надіслано",
            "Відповідь записано в цій демо-сесії."
        );
        r.Add(
            Card(
                new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        Body("ВІДПОВІДЬ НАДІСЛАНО", 14, true),
                        new Label
                        {
                            Text = request.Title,
                            FontFamily = "Manrope",
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = ThemeColor("#202823", "#F4F7FB"),
                        },
                        Body(proposal.Reply),
                        Body("Орієнтовна ціна: " + proposal.Price + " грн"),
                        Body("Точна адреса відкриється після вибору майстра.", 14, true),
                    },
                }
            )
        );
        r.Add(
            CreateButton("До доступних заявок", true, () =>
                _viewModel.CurrentScreen = "AvailableRequests"
            )
        );
        r.Add(CreateButton("Мої роботи", false, () => _viewModel.CurrentScreen = "Orders"));
        return r;
    }
}
