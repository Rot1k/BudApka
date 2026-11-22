namespace BudApka;

public partial class MainPage
{
    EquipmentRecord? _equipmentToShare;

    void AddRepairCategories(VerticalStackLayout page, Func<string, bool> isSelected,
        Action<string> select)
    {
        foreach (var group in RepairCatalog.Categories.GroupBy(category => category.Group))
        {
            if (group.Key.Length > 0)
            {
                page.Add(Body(group.Key, 14, true));
            }
            foreach (var category in group)
            {
                string? icon = category.Id switch
                {
                    "plumbing" => "water",
                    "electrical" => "bolt",
                    "appliances" => "appliance",
                    "handyman" or "construction" => "tools",
                    _ => null,
                };
                page.Add(SelectionRow(category.Name, isSelected(category.Id),
                    () => select(category.Id), icon));
            }
        }
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
