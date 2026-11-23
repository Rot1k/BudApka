using System.Globalization;

namespace BudApka;

public partial class MainPage
{
    EquipmentRecord? _equipmentToShare;

    void AddRepairCategories(VerticalStackLayout page, Func<string, bool> isSelected,
        Action<string> select, Dictionary<string, Action<bool>>? selectionUpdates = null)
    {
        foreach (var group in RepairCatalog.Categories.GroupBy(category => category.Group))
        {
            if (group.Key.Length > 0)
            {
                page.Add(Body(group.Key, 14, true));
            }
            foreach (var category in group)
            {
                page.Add(SelectionRow(category.Name, isSelected(category.Id),
                    () => select(category.Id), out var setSelected, RepairCategoryIcon(category.Id)));
                selectionUpdates?.Add(category.Id, setSelected);
            }
        }
    }

    static string? RepairCategoryIcon(string id) => id switch
    {
        "plumbing" => "water",
        "electrical" => "bolt",
        "appliances" => "appliance",
        "handyman" or "construction" => "tools",
        _ => null,
    };

    View MasterStatsView(string masterId)
    {
        var stats = _viewModel.GetMasterStats(masterId);
        string rating = stats.AverageRating is double average
            ? $"Середня оцінка: {average.ToString("0.0", CultureInfo.GetCultureInfo("uk-UA"))} із 5"
            : "Оцінок поки немає";
        return new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                WithIcon("star", Body(rating, 14)),
                Body($"Виконані замовлення: {stats.CompletedOrders}", 14),
            },
        };
    }

    static string EquipmentSummary(EquipmentRecord? item)
    {
        if (item == null)
        {
            return "Паспорт техніки не додано.";
        }

        return string.Join(Environment.NewLine, new[]
        {
            "Паспорт техніки · " + item.Name,
            item.Type.Length > 0 ? "Тип: " + item.Type : "",
            item.Model.Length > 0 ? "Модель: " + item.Model : "",
            item.Notes.Length > 0 ? "Нотатки: " + item.Notes : "",
        }.Where(value => value.Length > 0));
    }

    View AttachEquipment()
    {
        var item = _equipmentToShare;
        if (item == null)
        {
            return Equipment();
        }

        var page = Root("Прикріпити паспорт", "Виберіть, які дані побачить майстер.");
        page.Add(Body("Назва: " + item.Name));
        CheckBox AddField(string label, string value)
        {
            var check = new CheckBox();
            if (value.Length > 0)
            {
                var row = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(GridLength.Auto),
                        new ColumnDefinition(GridLength.Star),
                    },
                    ColumnSpacing = 8,
                };
                SemanticProperties.SetDescription(check, "Передати поле: " + label);
                row.Add(check, 0, 0);
                row.Add(Body(label + ": " + value), 1, 0);
                page.Add(row);
            }
            return check;
        }

        var type = AddField("Тип", item.Type);
        var model = AddField("Модель", item.Model);
        var notes = AddField("Нотатки", item.Notes);
        page.Add(CreateButton("Прикріпити до заявки", true, () =>
        {
            _viewModel.DraftEquipment = new EquipmentRecord(item.Name,
                type.IsChecked ? item.Type : "", model.IsChecked ? item.Model : "",
                notes.IsChecked ? item.Notes : "");
            _viewModel.CurrentScreen = "NewRequest";
        }));
        page.Add(CreateButton("Назад до техніки", false, () =>
            _viewModel.CurrentScreen = "Equipment"));
        return page;
    }
}
