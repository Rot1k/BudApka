using Microsoft.Maui.Controls.Shapes;

namespace BudApka;

public partial class MainPage
{
    string _equipmentReturnScreen = "Profile";
    EquipmentRecord? _editingEquipment;

    View Profile()
    {
        var r = Root("Профіль і адреси", "Один акаунт для замовлень і роботи майстром.");
        string phoneDraft = _viewModel.Phone;
        string nameDraft = _viewModel.ProfileName;
        r.Add(PhoneField(phoneDraft, value => phoneDraft = value, _viewModel.IsSignedIn));
        r.Add(
            LabeledField(
                "Ім’я · необов’язково", nameDraft, "Олег", null, v => nameDraft = v,
                validate: InputRules.ValidateName,
                allowCharacter: InputRules.NameCharacter,
                maxLength: 80
            )
        );
        r.Add(
            Card(
                new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        new Label
                        {
                            Text = "Збережені адреси",
                            FontFamily = "Manrope",
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = ThemeColor("#202823", "#F4F7FB"),
                        },
                        Body(string.IsNullOrEmpty(_viewModel.SavedAddress)
                            ? "Адресу можна зберегти за згодою під час заповнення заявки."
                            : _viewModel.SavedAddress),
                    },
                }
            )
        );
        r.Add(
            Card(
                new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        new Label
                        {
                            Text = "Моя техніка",
                            FontFamily = "Manrope",
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = ThemeColor("#202823", "#F4F7FB"),
                        },
                        Body("Довідкові дані обладнання — за бажанням."),
                        CreateButton("Перейти до техніки", false, () =>
                        {
                            _equipmentReturnScreen = "Profile";
                            _viewModel.CurrentScreen = "Equipment";
                        }),
                    },
                }
            )
        );
        r.Add(CreateAsyncButton("Зберегти профіль", true, async () =>
        {
            if (!ValidateFields())
            {
                return;
            }

            InputRules.TryNormalizePhone(phoneDraft, out var phone);
            _viewModel.Phone = phone;
            _viewModel.ProfileName = nameDraft.Trim();
            await DisplayAlertAsync(
                "Профіль збережено", "Зміни доступні в цій демо-сесії.", "Готово");
        }));
        r.Add(
            CreateButton("Працювати майстром", false, () =>
                _viewModel.CurrentScreen = "MasterActivation"
            )
        );
        return r;
    }

    View Equipment()
    {
        var r = Root(
            "Моя техніка",
            "Добровільний довідник для ремонту. Заявку можна створити без нього."
        );
        r.Add(CreateButton(_equipmentReturnScreen == "NewRequest" ? "Назад до заявки"
            : "Назад", false, () => _viewModel.CurrentScreen = _equipmentReturnScreen));
        foreach (var item in _viewModel.Equipment)
        {
            var card = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    Body("ЗБЕРЕЖЕНИЙ ПРИЛАД", 14, true),
                    new Label
                    {
                        Text = item.Name,
                        FontFamily = "Manrope",
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = ThemeColor("#202823", "#F4F7FB"),
                    },
                    Body(
                        string.Join(
                            Environment.NewLine,
                            "Тип: " + item.Type,
                            "Модель: " + item.Model
                        )
                    ),
                    Body("Передача даних майстру — тільки з вашого дозволу.", 14, true),
                    CreateButton("Переглянути й редагувати", false, () =>
                    {
                        OpenEquipmentForm(item);
                    }),
                },
            };
            if (!string.IsNullOrWhiteSpace(item.Notes))
            {
                card.Insert(card.Children.Count - 2, Body("Додаткова інформація", 14, true));
                card.Insert(card.Children.Count - 2, Body(item.Notes));
            }
            if (_equipmentReturnScreen == "NewRequest")
            {
                card.Add(CreateButton("Вибрати для заявки", true, () =>
                {
                    _equipmentToShare = item;
                    _viewModel.CurrentScreen = "AttachEquipment";
                }));
            }
            r.Add(Card(card));
        }
        r.Add(
            CreateButton("Додати техніку вручну", true, () => OpenEquipmentForm(null), "plus")
        );
        r.Add(
            Body(
                string.Join(
                    " ",
                    "Розпізнавання шильдика — додаткова можливість на майбутнє.",
                    "Ручне введення доступне завжди."
                ),
                13,
                true
            )
        );
        return r;
    }

    void OpenEquipmentForm(EquipmentRecord? item)
    {
        _editingEquipment = item;
        _viewModel.EquipmentName = item?.Name ?? "";
        _viewModel.EquipmentType = item?.Type ?? "";
        _viewModel.EquipmentModel = item?.Model ?? "";
        _viewModel.EquipmentNotes = item?.Notes ?? "";
        _viewModel.CurrentScreen = "EquipmentForm";
    }

    View EquipmentForm()
    {
        var r = Root("Паспорт техніки", "Довідкові дані для точнішого опису ремонту.");
        r.Add(
            LabeledField("Назва приладу", _viewModel.EquipmentName, "Змішувач на кухні", null, v =>
                _viewModel.EquipmentName = v,
                validate: value => Required(value, "Вкажіть назву приладу.")
            )
        );
        r.Add(
            LabeledField("Тип", _viewModel.EquipmentType, "Сантехнічне обладнання", null, v =>
                _viewModel.EquipmentType = v
            )
        );
        r.Add(
            LabeledField("Модель", _viewModel.EquipmentModel, "Grohe Start", null, v =>
                _viewModel.EquipmentModel = v
            )
        );
        const string notesLabel = "Додаткова інформація · необов’язково";
        r.Add(Body(notesLabel, 14));
        var notes = FieldEditor(
            "Наприклад: серійний номер, особливості або попередні ремонти",
            value => _viewModel.EquipmentNotes = value);
        notes.Text = _viewModel.EquipmentNotes;
        SemanticProperties.SetDescription(notes, notesLabel);
        r.Add(notes);
        r.Add(
            Card(
                new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        new Label
                        {
                            Text = "Передача даних майстру",
                            FontFamily = "Manrope",
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = ThemeColor("#202823", "#F4F7FB"),
                        },
                        Body("Дані будуть передані лише після вашого окремого дозволу."),
                    },
                }
            )
        );
        r.Add(CreateButton("Зберегти техніку", true, () =>
            {
                if (!ValidateFields())
                {
                    return;
                }

                var item = new EquipmentRecord(
                    _viewModel.EquipmentName.Trim(),
                    _viewModel.EquipmentType.Trim(),
                    _viewModel.EquipmentModel.Trim(),
                    _viewModel.EquipmentNotes.Trim());
                int index = _viewModel.Equipment.FindIndex(entry =>
                    ReferenceEquals(entry, _editingEquipment));
                if (index < 0)
                {
                    _viewModel.Equipment.Add(item);
                }
                else
                {
                    _viewModel.Equipment[index] = item;
                }

                _viewModel.CurrentScreen = "Equipment";
            }));
        r.Add(
            CreateButton("Назад до техніки", false, () => _viewModel.CurrentScreen = "Equipment")
        );
        return r;
    }
}
