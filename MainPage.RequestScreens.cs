using Microsoft.Maui.Controls.Shapes;

namespace BudApka;

public partial class MainPage
{
    Func<bool>? _validateRequestDetails;

    View Home()
    {
        var r = Root(
            "Майстер для вашого дому",
            "Опишіть проблему своїми словами. Ви самі оберете, кому довірити ремонт."
        );
#if DEBUG
        if (!_viewModel.HasDebugData)
        {
            r.Add(CreateButton("Заповнити тестовими даними", false,
                _viewModel.SeedDebugData, "tools"));
        }
#endif
        r.Add(CreateButton(
            _viewModel.HasRequestDraft ? "Продовжити заявку" : "Створити заявку",
            true,
            () => _viewModel.CurrentScreen = "NewRequest",
            "plus"
        ));
        if (_viewModel.HasOrder)
        {
            r.Add(CreateButton("Мої замовлення", false, () =>
                _viewModel.CurrentScreen = "Orders", "request"));
        }

        if (_viewModel.HasRequestDraft)
        {
            r.Add(CreateAsyncButton("Почати нову заявку", false,
                () => ReplaceDraftAsync(startNew: true), "plus"));
            r.Add(CreateAsyncButton("Видалити чернетку", false,
                () => ReplaceDraftAsync(startNew: false)));
        }
        r.Add(CreateButton("Знайти майстра", false, () =>
            _viewModel.CurrentScreen = "ServiceCatalog", "search"));
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

    async Task ReplaceDraftAsync(bool startNew, ServiceCard? service = null)
    {
        string title = startNew ? "Почати нову заявку?" : "Видалити чернетку?";
        string message = startNew
            ? "Поточну чернетку буде видалено, і відкриється нова заявка."
            : "Дані цієї чернетки буде видалено.";
        bool confirmed = await DisplayAlertAsync(title, message,
            startNew ? "Почати нову" : "Видалити", "Скасувати");
        if (!confirmed)
        {
            return;
        }

        _viewModel.ClearRequestDraft();
        if (startNew)
        {
            OpenNewRequest(service);
        }
        else
        {
            _viewModel.NotifyAll();
        }
    }

    View NewRequest()
    {
        var r = Root(
            "Що сталося?",
            "1 із 3 · Категорія й симптоми. Не знаєте точної назви — оберіть найближчий варіант."
        );
        _viewModel.HasRequestDraft = true;
        var categoryRows = new Dictionary<string, Action<bool>>();
        var categoryError = ValidationMessage();
        var otherError = ValidationMessage();
        var descriptionError = ValidationMessage();
        var categoryDetails = new VerticalStackLayout { Spacing = r.Spacing };
        r.Add(Body("Категорія ремонту", 18));
        r.Add(categoryError);
        var categoryList = new VerticalStackLayout { Spacing = r.Spacing };
        var categorySummary = new VerticalStackLayout { Spacing = 8 };
        r.Add(categorySummary);
        r.Add(categoryList);
        AddRepairCategories(categoryList, id => _viewModel.CategoryId == id, id =>
        {
            string previousCategory = _viewModel.CategoryId;
            if (previousCategory != id)
            {
                _viewModel.SelectCategory(id);
                if (categoryRows.TryGetValue(previousCategory, out var deselect))
                {
                    deselect(false);
                }
                categoryRows[id](true);
                categoryError.IsVisible = false;
                descriptionError.IsVisible = false;
                UpdateCategoryDetails();
            }

            SetCategoryListExpanded(false);
            if (BodyHost.Content is ScrollView scroll)
            {
                _ = scroll.ScrollToAsync(0, 0, false);
            }
        }, categoryRows);
        SetCategoryListExpanded(_viewModel.CategoryId.Length == 0);
        var problem = new VerticalStackLayout { Spacing = r.Spacing };
        problem.Add(Body("Проблема або потрібні роботи", 18));
        problem.Add(categoryDetails);
        UpdateCategoryDetails();

        problem.Add(Body("Опис своїми словами", 14));
        problem.Add(descriptionError);
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
        description.TextChanged += (_, e) =>
        {
            _viewModel.Description = e.NewTextValue;
            if (!string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                descriptionError.IsVisible = false;
            }
        };
        problem.Add(description);
        r.Add(Card(problem));
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
                if (file != null && BodyHost.Content is ScrollView { Content: var content }
                    && content == r)
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
        _validateRequestDetails = ValidateRequestDetails;
        return r;

        void SetCategoryListExpanded(bool expanded)
        {
            categorySummary.Children.Clear();
            if (!expanded && RepairCatalog.Find(_viewModel.CategoryId) is { } category)
            {
                categorySummary.Add(SelectionRow(category.Name, true,
                    () => SetCategoryListExpanded(true), RepairCategoryIcon(category.Id) ?? "tools"));
                categorySummary.Add(CreateButton("Змінити категорію", false,
                    () => SetCategoryListExpanded(true)));
            }
            categorySummary.IsVisible = !expanded;
            categoryList.IsVisible = expanded;
        }

        void UpdateCategoryDetails()
        {
            categoryDetails.Children.Clear();
            otherError.IsVisible = false;
            if (_viewModel.CategoryId == RepairCatalog.Other)
            {
                categoryDetails.Add(LabeledField("Назва категорії", _viewModel.OtherCategory,
                    "Наприклад, ремонт ролет", null, value =>
                    {
                        _viewModel.OtherCategory = value;
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            otherError.IsVisible = false;
                        }
                    }, maxLength: 80));
                categoryDetails.Add(otherError);
            }

            var category = RepairCatalog.Find(_viewModel.CategoryId);
            if (category is not { Suggestions.Length: > 0 })
            {
                categoryDetails.IsVisible = categoryDetails.Children.Count > 0;
                return;
            }

            categoryDetails.IsVisible = true;
            categoryDetails.Add(Body(
                category.Planned ? "Які роботи потрібні?" : "Що ви помітили?", 14));
            foreach (string suggestion in category.Suggestions)
            {
                Action<bool> updateSelection = _ => { };
                categoryDetails.Add(SelectionRow(suggestion,
                    _viewModel.Symptoms.Contains(suggestion), () =>
                    {
                        bool selected = _viewModel.Symptoms.Add(suggestion);
                        if (!selected)
                        {
                            _viewModel.Symptoms.Remove(suggestion);
                        }
                        updateSelection(selected);
                        if (selected)
                        {
                            descriptionError.IsVisible = false;
                        }
                    }, out updateSelection));
            }
        }

        bool ValidateRequestDetails()
        {
            string? error = _viewModel.DraftError;
            if (error == null)
            {
                return true;
            }

            Label target = _viewModel.CategoryId.Length == 0 ? categoryError
                : _viewModel.Category.Length == 0 ? otherError : descriptionError;
            target.Text = error;
            target.IsVisible = true;
            ScrollToValidation(target);
            return false;
        }
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
                ValidateAmount,
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
        if (_viewModel.DraftServiceId != null)
        {
            r.Add(Body("Якщо вибрана послуга відповідає категорії та адресі, "
                + "майстер отримає запрошення після публікації заявки.", 14));
        }
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
        if (_viewModel.CurrentOrder is not { } order)
        {
            return Orders();
        }


        var r = Root("Заявку створено", "Можна переглянути демонстраційні відповіді майстрів.");
        var status = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                StatusBadge(order.Status),
                Body($"Заявка №{order.Request.Id}", 14, true),
                new Label
                {
                    Text = order.Request.Category,
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
        AddServiceRecommendations(r, order);
        r.Add(
            CreateButton("Переглянути мої замовлення", true, () =>
                _viewModel.CurrentScreen = "Orders"
            )
        );
        return r;
    }

    View Responses()
    {
        if (_viewModel.CurrentOrder is not { } order)
        {
            return Orders();
        }

        var r = Root(
            "Оберіть свого майстра",
            $"Заявка №{order.Request.Id} · Чат відкриється після вашого вибору."
        );
        AddServiceRecommendations(r, order);
        if (order.Offers.Count == 0)
        {
            r.Add(Card(Body("Пропозицій поки немає.")));
        }
        else
        {
            r.Add(Body("Порядок пропозицій: спочатку найновіші.", 14, true));
        }
        foreach (var offer in order.Offers.AsEnumerable().Reverse())
        {
            var card = new VerticalStackLayout { Spacing = 12 };
            if (offer.Id != PrototypeViewModel.LocalMasterId)
            {
                card.Add(Body("Демонстраційна пропозиція", 14, true));
            }
            if (offer.PhotoAsset.Length > 0)
            {
                card.Add(new Image
                {
                    Source = offer.PhotoAsset,
                    WidthRequest = 72,
                    HeightRequest = 72,
                    HorizontalOptions = LayoutOptions.Start,
                    Aspect = Aspect.AspectFill,
                });
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
            card.Add(MasterStatsView(offer.Id));
            card.Add(Body(offer.Message, 14, true));
            if (offer.Price.Length > 0)
            {
                card.Add(Body(offer.Price + " · довідкова ціна", 16));
            }
            card.Add(CreateButton("Профіль і відгуки", false, () =>
                {
                    order.PreviewedMaster = offer;
                    _viewModel.CurrentScreen = "MasterProfile";
                }));
            r.Add(Card(card));
            r.Add(
                CreateAsyncButton("Обрати " + offer.Name.Split(' ')[0], true, () =>
                    ConfirmMasterAsync(order, offer)
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
