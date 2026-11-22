using Microsoft.Maui.Controls.Shapes;

namespace BudApka;

public partial class MainPage
{
    Label? _draftValidation;

    View Home()
    {
        var r = Root(
            "Майстер для вашого дому",
            "Опишіть проблему своїми словами. Ви самі оберете, кому довірити ремонт."
        );
        r.Add(CreateButton(
            _viewModel.HasOrder ? "Мої замовлення"
                : _viewModel.HasRequestDraft ? "Продовжити заявку" : "Створити заявку",
            true,
            () => _viewModel.CurrentScreen = _viewModel.HasOrder ? "Orders" : "NewRequest"
        ));
        if (!_viewModel.HasOrder && _viewModel.HasRequestDraft)
        {
            r.Add(Body("Заповнені дані доступні в чернетці цієї сесії.", 14, true));
        }
        var how = new VerticalStackLayout { Spacing = 8 };
        how.Add(
            new Label
            {
                Text = "ЯК ЦЕ ПРАЦЮЄ",
                FontFamily = "Inter",
                FontSize = 14,
                TextColor = ThemeColor("#245E8B", "#77B5E8"),
            }
        );
        how.Add(
            new Label
            {
                Text = "Від проблеми до візиту",
                FontFamily = "Manrope",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = ThemeColor("#202823", "#F4F7FB"),
            }
        );
        how.Add(
            Body(
                string.Join(
                    Environment.NewLine,
                    "1. Опишіть несправність",
                    "2. Порівняйте відповіді й оберіть майстра",
                    "3. Обговоріть деталі в чаті"
                )
            )
        );
        r.Add(
            new Border
            {
                BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
                Stroke = ThemeColor("#E3E7DF", "#344151"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = 16,
                Content = how,
            }
        );
        var equipment = new VerticalStackLayout { Spacing = 10 };
        equipment.Add(
            new Label
            {
                Text = "Моя техніка",
                FontFamily = "Manrope",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = ThemeColor("#202823", "#F4F7FB"),
            }
        );
        equipment.Add(
            Body(
                "Збережіть дані приладу, якщо це зручно. Паспорт не потрібен для створення заявки."
            )
        );
        equipment.Add(
            CreateButton("Переглянути техніку", false, () =>
            {
                _equipmentReturnScreen = "Home";
                _viewModel.CurrentScreen = "Equipment";
            })
        );
        r.Add(
            new Border
            {
                BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
                Stroke = ThemeColor("#E3E7DF", "#344151"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = 16,
                Content = equipment,
            }
        );
        r.Add(Body("Точну адресу побачить лише обраний майстер.", 14, true));
        return r;
    }

    View NewRequest()
    {
        var r = Root(
            "Що сталося?",
            "1 із 3 · Категорія й симптоми. Не знаєте точної назви — оберіть найближчий варіант."
        );
        _viewModel.HasRequestDraft = true;
        AddRepairCategories(r, id => _viewModel.CategoryId == id, id =>
        {
            _viewModel.SelectCategory(id);
            _viewModel.NotifyAll();
        });
        if (_viewModel.CategoryId == RepairCatalog.Other)
        {
            r.Add(LabeledField("Назва категорії", _viewModel.OtherCategory,
                "Наприклад, ремонт ролет", null, value => _viewModel.OtherCategory = value,
                maxLength: 80));
        }

        var category = RepairCatalog.Find(_viewModel.CategoryId);
        if (category is { Suggestions.Length: > 0 })
        {
            r.Add(Body(category.Planned ? "Які роботи потрібні?" : "Що ви помітили?", 14));
            foreach (string suggestion in category.Suggestions)
            {
                r.Add(SelectionRow(suggestion, _viewModel.Symptoms.Contains(suggestion), () =>
                {
                    if (!_viewModel.Symptoms.Add(suggestion))
                    {
                        _viewModel.Symptoms.Remove(suggestion);
                    }
                    _viewModel.NotifyAll();
                }));
            }
        }

        r.Add(Body("Опис своїми словами", 14));
        var description = new Editor
        {
            Text = _viewModel.Description,
            Placeholder = "Що потрібно зробити або що сталося?",
            FontFamily = "Inter",
            FontSize = 16,
            TextColor = ThemeColor("#202823", "#F4F7FB"),
            PlaceholderColor = ThemeColor("#566257", "#BFCADA"),
            BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
            HeightRequest = 92,
        };
        description.TextChanged += (_, e) => _viewModel.Description = e.NewTextValue;
        r.Add(description);
        var photo = new VerticalStackLayout { Spacing = 10 };
        photo.Add(
            new Label
            {
                Text = "Фото · необов’язково",
                FontFamily = "Inter",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = ThemeColor("#202823", "#F4F7FB"),
            }
        );
        bool hasPhoto = !string.IsNullOrEmpty(_viewModel.PhotoPath);
        if (hasPhoto)
        {
            photo.Add(new Image
            {
                Source = ImageSource.FromFile(_viewModel.PhotoPath),
                HeightRequest = 120,
                Aspect = Aspect.AspectFit,
            });
        }
        photo.Add(CreateAsyncButton(hasPhoto ? "Замінити фото" : "Додати фото", false, async () =>
        {
            try
            {
                var files = await MediaPicker.Default.PickPhotosAsync();
                var file = files.FirstOrDefault();
                if (file != null)
                {
                    _viewModel.PhotoPath = file.FullPath;
                    _viewModel.NotifyAll();
                }
            }
            catch (Exception exception) when (exception is FeatureNotSupportedException
                or PermissionException or IOException)
            {
                await DisplayAlertAsync(
                    "Фото не додано",
                    "Перевірте доступ до фото або продовжте заявку без фото.",
                    "Готово");
            }
        }, "photo"));
        if (hasPhoto)
        {
            photo.Add(CreateButton("Прибрати фото", false, () =>
            {
                _viewModel.PhotoPath = "";
                _viewModel.NotifyAll();
            }));
        }
        r.Add(
            new Border
            {
                BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
                Stroke = ThemeColor("#E3E7DF", "#344151"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = 16,
                Content = photo,
            }
        );
        var equipment = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label
                {
                    Text = "Дані техніки · необов’язково",
                    FontFamily = "Inter",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ThemeColor("#202823", "#F4F7FB"),
                },
                Body(EquipmentSummary(_viewModel.DraftEquipment), 14, true),
                CreateButton(_viewModel.DraftEquipment == null ? "Прикріпити паспорт"
                    : "Замінити паспорт", false, () =>
                {
                    _equipmentReturnScreen = "NewRequest";
                    _viewModel.CurrentScreen = "Equipment";
                }),
            },
        };
        if (_viewModel.DraftEquipment != null)
        {
            equipment.Add(CreateButton("Від’єднати паспорт", false, () =>
            {
                _viewModel.DraftEquipment = null;
                _viewModel.NotifyAll();
            }));
        }
        _draftValidation = Body("", 14);
        _draftValidation.TextColor = ThemeColor("#9B201B", "#FFB4AB");
        _draftValidation.IsVisible = false;
        r.Add(_draftValidation);
        r.Add(
            new Border
            {
                BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
                Stroke = ThemeColor("#E3E7DF", "#344151"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = 16,
                Content = equipment,
            }
        );
        return r;
    }

    View Address()
    {
        var r = Root(
            "Де потрібна допомога?",
            "2 із 3 · Адреса й необов’язковий бюджет. Час обговорите після вибору майстра."
        );
        AddAddressFields(r);
        var save = new Switch
        {
            IsToggled = _viewModel.SaveAddress,
            OnColor = Color.FromArgb(IsDarkTheme ? "#E3A600" : "#FFB800"),
            ThumbColor = ThemeColor("#FFFFFF", "#F4F7FB"),
            VerticalOptions = LayoutOptions.Center,
        };
        save.Toggled += (_, e) => _viewModel.SaveAddress = e.Value;
        r.Add(
            new HorizontalStackLayout
            {
                Spacing = 10,
                Padding = new Thickness(16, 4),
                Children = { save, Body("Зберегти адресу в профілі", 14) },
            }
        );
        r.Add(
            LabeledField(
                "Бюджет · необов’язково",
                _viewModel.Budget,
                "1 500",
                "Ваш орієнтир, не розрахована вартість.",
                v => _viewModel.Budget = v,
                Keyboard.Numeric,
                "грн",
                v => string.IsNullOrWhiteSpace(v) ? null : ValidateAmount(v),
                allowCharacter: c => InputRules.Digit(c) || c == ' ',
                maxLength: 14
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
                            Text = "Коли відбудеться візит?",
                            FontFamily = "Manrope",
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = ThemeColor("#202823", "#F4F7FB"),
                        },
                        Body(
                            string.Join(
                                " ",
                                "Дату й зручний проміжок часу обговорите в чаті",
                                "та зазначите в деталях візиту."
                            )
                        ),
                    },
                }
            )
        );
        r.Add(CreateButton("Назад до опису", false, () =>
            _viewModel.CurrentScreen = "NewRequest"));
        return r;
    }

    View ReviewRequest()
    {
        var r = Root("Усе правильно?", "3 із 3 · Перевірка перед надсиланням.");
        var draft = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Body(
                    string.Join(
                        Environment.NewLine,
                        "Категорія: " + _viewModel.Category,
                        "Ознаки або роботи: " + (_viewModel.Symptoms.Count == 0
                            ? "не вибрано" : string.Join(", ", _viewModel.Symptoms)),
                        "Опис: " + (string.IsNullOrWhiteSpace(_viewModel.Description)
                            ? "не додано" : _viewModel.Description),
                        "Фото: " + (string.IsNullOrEmpty(_viewModel.PhotoPath)
                            ? "не додано" : "додано"),
                        "Населений пункт — " + _viewModel.District,
                        "Адреса для візиту — " + _viewModel.ExactAddress,
                        "Ваш бюджет: " + (string.IsNullOrWhiteSpace(_viewModel.Budget)
                            ? "не вказано" : _viewModel.Budget + " грн")
                    ),
                    14,
                    true
                ),
            },
        };
        r.Add(Card(draft));
        r.Add(
            Card(
                new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        new Label
                        {
                            Text = "Приватність",
                            FontFamily = "Manrope",
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = ThemeColor("#202823", "#F4F7FB"),
                        },
                        Body(
                            string.Join(
                                " ",
                                "До вибору майстри бачать місцевість без вулиці й номера будинку.",
                                "Адресу не буде збережено без окремої згоди."
                            )
                        ),
                    },
                }
            )
        );
        r.Add(Card(Body(EquipmentSummary(_viewModel.DraftEquipment), 14)));
        r.Add(
            CreateButton(_viewModel.IsSignedIn ? "Надіслати заявку" : "Увійти й надіслати",
                true, () =>
                {
                    if (_viewModel.IsSignedIn)
                    {
                        _viewModel.SubmitRequest();
                    }
                    else
                    {
                        _viewModel.CurrentScreen = "SignIn";
                    }
                })
        );
        r.Add(CreateButton("Змінити адресу", false, () => _viewModel.CurrentScreen = "Address"));
        r.Add(CreateButton("Змінити опис", false, () => _viewModel.CurrentScreen = "NewRequest"));
        return r;
    }

    View SignIn()
    {
        var r = Root("Збережімо вашу заявку", "Чернетка залишиться на місці після входу.");
        r.Add(PhoneField(_viewModel.Phone, value => _viewModel.Phone = value, required: true));
        r.Add(
            LabeledField(
                "Демо-код",
                _viewModel.Code,
                "1234",
                "Для демонстрації введіть 1234.",
                v => _viewModel.Code = v,
                Keyboard.Numeric,
                validate: v => v == "1234" ? null : "Введіть демо-код 1234.",
                allowCharacter: InputRules.Digit,
                maxLength: 4
            )
        );
        r.Add(Body("Демонстраційний вхід: реальне SMS не надсилається.", 14, true));
        r.Add(
            Card(
                new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        new Label
                        {
                            Text = "Дані не загубляться",
                            FontFamily = "Manrope",
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = ThemeColor("#202823", "#F4F7FB"),
                        },
                        Body("Після підтвердження ви повернетеся до готової заявки."),
                    },
                }
            )
        );
        r.Add(CreateButton("Підтвердити й надіслати", true, () =>
        {
            if (ValidateFields() && InputRules.TryNormalizePhone(_viewModel.Phone, out var phone))
            {
                _viewModel.Phone = phone;
                _viewModel.IsSignedIn = true;
                _viewModel.SubmitRequest();
            }
        }));
        r.Add(
            CreateButton("Назад до чернетки", false, () =>
                _viewModel.CurrentScreen = "ReviewRequest"
            )
        );
        return r;
    }

    View Submitted()
    {
        var r = Root("Заявку створено", "Можна переглянути демонстраційні відповіді майстрів.");
        var status = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                StatusBadge(_viewModel.Status),
                Body("Заявка №2048", 14, true),
                new Label
                {
                    Text = _viewModel.OrderCategory,
                    FontFamily = "Manrope",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ThemeColor("#202823", "#F4F7FB"),
                },
                Body("Майстри бачать категорію, опис і населений пункт із регіоном."),
            },
        };
        r.Add(
            new Border
            {
                BackgroundColor = Color.FromArgb(IsDarkTheme ? "#244D38" : "#EAF4ED"),
                Stroke = ThemeColor("#E3E7DF", "#344151"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 16 },
                Padding = 16,
                Content = status,
            }
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
                            Text = "Ви обираєте самі",
                            FontFamily = "Manrope",
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = ThemeColor("#202823", "#F4F7FB"),
                        },
                        Body(
                            string.Join(
                                " ",
                                "Профілі, відгуки й відповіді допоможуть порівняти варіанти.",
                                "Автопризначення немає."
                            )
                        ),
                    },
                }
            )
        );
        r.Add(
            CreateButton("Переглянути мої замовлення", true, () =>
                _viewModel.CurrentScreen = "Orders"
            )
        );
        return r;
    }

    View Responses()
    {
        var r = Root(
            "Оберіть свого майстра",
            $"Заявка №2048 · {_viewModel.Offers.Count} демо-відповіді. "
                + "Чат відкриється після вашого вибору."
        );
        foreach (var offer in _viewModel.Offers)
        {
            var card = new VerticalStackLayout { Spacing = 12 };
            var top = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(72),
                    new ColumnDefinition(GridLength.Star),
                },
                ColumnSpacing = 12,
            };
            top.Add(
                new Border
                {
                    WidthRequest = 72,
                    HeightRequest = 72,
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 16 },
                    Content = new Image { Source = offer.PhotoAsset, Aspect = Aspect.AspectFill },
                },
                0,
                0
            );
            var meta = new VerticalStackLayout
            {
                Spacing = 8,
                VerticalOptions = LayoutOptions.Center,
            };
            meta.Add(
                new Border
                {
                    BackgroundColor = Color.FromArgb(IsDarkTheme ? "#244D38" : "#EAF4ED"),
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Padding = new Thickness(8, 6),
                    Content = Body("Є відгуки", 14, false),
                }
            );
            meta.Add(WithIcon("star", Body(offer.Rating + " · " + offer.Reviews + " відгуків", 14)));
            top.Add(meta, 1, 0);
            if (offer.Id != PrototypeViewModel.LocalMasterId)
            {
                card.Add(top);
            }
            card.Add(
                new Label
                {
                    Text = offer.Name,
                    FontFamily = "Manrope",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ThemeColor("#202823", "#F4F7FB"),
                }
            );
            card.Add(Body(offer.Id == PrototypeViewModel.LocalMasterId ? offer.Specialty
                : $"{offer.Specialty} · {offer.Years} років досвіду"));
            card.Add(Body(offer.Message, 14, true));
            card.Add(Body(offer.Price + " · довідкова ціна", 16));
            card.Add(CreateButton("Профіль і відгуки", false, () =>
                {
                    _viewModel.PreviewedMaster = offer;
                    _viewModel.CurrentScreen = "MasterProfile";
                }));
            r.Add(Card(card));
            r.Add(
                CreateAsyncButton("Обрати " + offer.Name.Split(' ')[0], true, () =>
                    ConfirmMasterAsync(offer)
                )
            );
        }
        r.Add(
            Body(
                "Ціни наведені майстрами для ознайомлення. Оплати й автопризначення немає.",
                13,
                true
            )
        );
        return r;
    }
}
