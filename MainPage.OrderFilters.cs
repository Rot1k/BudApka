using Microsoft.Maui.Controls.Shapes;

namespace BudApka;

public partial class MainPage
{
    enum OrderScope
    {
        Active,
        History,
        All,
    }

    sealed class OrderFilter
    {
        public OrderScope Scope { get; set; }
        public string CategoryId { get; set; } = "";
        public HashSet<OrderStatus> Statuses { get; } = new();
        public bool IsExpanded { get; set; }
        public bool HasCriteria => CategoryId.Length > 0 || Statuses.Count > 0;

        public bool Matches(string categoryId, OrderStatus status)
        {
            bool closed = status is OrderStatus.Completed or OrderStatus.Cancelled;
            return (Scope == OrderScope.All || closed == (Scope == OrderScope.History))
                && (CategoryId.Length == 0 || CategoryId == categoryId)
                && (Statuses.Count == 0 || Statuses.Contains(status));
        }

        public void Reset()
        {
            Scope = OrderScope.Active;
            CategoryId = "";
            Statuses.Clear();
        }

        public void CopyCriteria(OrderFilter source)
        {
            CategoryId = source.CategoryId;
            Statuses.Clear();
            Statuses.UnionWith(source.Statuses);
        }
    }

    readonly OrderFilter _clientOrdersFilter = new();
    readonly OrderFilter _masterOrdersFilter = new();
    readonly OrderFilter _availableOrdersFilter = new();

    static readonly OrderStatus[] ActiveStatuses =
    [
        OrderStatus.Submitted, OrderStatus.HasResponses, OrderStatus.MasterSelected,
        OrderStatus.VisitRecorded,
    ];
    static readonly OrderStatus[] HistoryStatuses = [OrderStatus.Completed, OrderStatus.Cancelled];

    View StatusBadge(OrderStatus status)
    {
        var (foreground, background) = status switch
        {
            OrderStatus.HasResponses => (ThemeColor("#805600", "#FFD789"),
                ThemeColor("#FFF3CF", "#3B3423")),
            OrderStatus.MasterSelected or OrderStatus.VisitRecorded or OrderStatus.Completed =>
                (ThemeColor("#24663D", "#B6E5C5"), ThemeColor("#EAF4ED", "#244D38")),
            OrderStatus.Cancelled => (ThemeColor("#B33232", "#FFB4AB"),
                ThemeColor("#FCEDED", "#542725")),
            _ => (ThemeColor("#245E8B", "#A8D5F8"), ThemeColor("#EBF3FA", "#24384B")),
        };
        return StateBadge(PrototypeViewModel.OrderStatusLabel(status), foreground, background);
    }

    View StateBadge(string text, Color foreground, Color background)
    {
        var label = Body(text, 14);
        label.TextColor = foreground;
        return new Border
        {
            Content = label,
            HorizontalOptions = LayoutOptions.Start,
            Padding = new Thickness(10, 6),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            BackgroundColor = background,
        };
    }

    void AddOrderFilters(VerticalStackLayout page, OrderFilter filter)
    {
        var scopes = new FlexLayout
        {
            Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,
        };
        foreach (var (scope, label) in new[]
        {
            (OrderScope.Active, "Активні"),
            (OrderScope.History, "Історія"),
            (OrderScope.All, "Усі"),
        })
        {
            scopes.Add(FilterChip(label, filter.Scope == scope, () =>
            {
                filter.Scope = scope;
                if (scope == OrderScope.Active)
                {
                    filter.Statuses.ExceptWith(HistoryStatuses);
                }
                else if (scope == OrderScope.History)
                {
                    filter.Statuses.ExceptWith(ActiveStatuses);
                }
                _viewModel.NotifyAll();
            }));
        }
        page.Add(scopes);
        page.Add(CreateButton(filter.IsExpanded ? "Сховати фільтри" : "Фільтри", false, () =>
        {
            filter.IsExpanded = !filter.IsExpanded;
            _viewModel.NotifyAll();
        }, filter.IsExpanded ? "chevron" : "filter"));
        if (filter.IsExpanded)
        {
            IEnumerable<OrderStatus> statuses = filter.Scope switch
            {
                OrderScope.Active => ActiveStatuses,
                OrderScope.History => HistoryStatuses,
                _ => ActiveStatuses.Concat(HistoryStatuses),
            };
            AddOrderCriteria(page, filter, statuses, _viewModel.NotifyAll);
        }
        AddFilterSummary(page, filter);
        if (filter.Scope != OrderScope.Active || filter.HasCriteria)
        {
            AddResetFilters(page, () =>
            {
                filter.Reset();
                _viewModel.NotifyAll();
            });
        }
    }

    void AddOrderCriteria(VerticalStackLayout page, OrderFilter filter,
        IEnumerable<OrderStatus> statuses, Action? update = null)
    {
        page.Add(Body("Категорія ремонту", 14));
        var categories = RepairCatalog.Categories.ToArray();
        var picker = new Picker
        {
            ItemsSource = new[] { "Усі категорії" }.Concat(categories.Select(c => c.Name))
                .ToArray(),
            SelectedIndex = Array.FindIndex(categories, item => item.Id == filter.CategoryId) + 1,
            TextColor = ThemeColor("#202823", "#F4F7FB"),
            BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
            MinimumHeightRequest = 48,
        };
        picker.SelectedIndexChanged += (_, _) =>
        {
            filter.CategoryId = picker.SelectedIndex > 0 ? categories[picker.SelectedIndex - 1].Id
                : "";
            update?.Invoke();
        };
        page.Add(picker);
        page.Add(Body("Статуси", 14));
        var chips = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        foreach (var status in statuses)
        {
            string label = PrototypeViewModel.OrderStatusLabel(status);
            var chip = FilterChip(label, filter.Statuses.Contains(status));
            BindButtonAction(chip, () =>
            {
                if (!filter.Statuses.Add(status))
                {
                    filter.Statuses.Remove(status);
                }
                SetFilterChipState(chip, filter.Statuses.Contains(status));
                update?.Invoke();
            });
            chips.Add(chip);
        }
        page.Add(chips);
    }

    Button FilterChip(string label, bool selected, Action? action = null)
    {
        var button = ButtonStyle(label, false);
        button.Margin = new Thickness(0, 0, 8, 6);
        button.MinimumHeightRequest = 44;
        button.Padding = new Thickness(12, 6);
        SetFilterChipState(button, selected);
        if (action != null)
        {
            BindButtonAction(button, action);
        }
        return button;
    }

    void SetFilterChipState(Button button, bool selected)
    {
        button.BackgroundColor = selected ? ThemeColor("#FFB800", "#E3A600")
            : ThemeColor("#FFFFFF", "#202833");
        button.TextColor = selected ? Color.FromArgb("#202823")
            : ThemeColor("#202823", "#F4F7FB");
        button.BorderColor = selected ? button.BackgroundColor
            : ThemeColor("#727D72", "#91A1B7");
        SemanticProperties.SetDescription(button,
            selected ? button.Text + ", вибрано" : button.Text);
    }

    void AddFilterSummary(VerticalStackLayout page, OrderFilter filter)
    {
        var labels = filter.Statuses.Order().Select(PrototypeViewModel.OrderStatusLabel).ToList();
        if (RepairCatalog.Find(filter.CategoryId) is { } category)
        {
            labels.Insert(0, category.Name);
        }
        if (labels.Count > 0)
        {
            page.Add(Body("Фільтри: " + string.Join(" · ", labels), 14, true));
        }
    }

    void AddResetFilters(VerticalStackLayout page, Action reset) =>
        page.Add(CreateButton("Скинути фільтри", false, reset, "close"));
}
