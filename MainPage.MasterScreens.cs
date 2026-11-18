using Microsoft.Maui.Controls.Shapes;

namespace BudApka;

public partial class MainPage
{
    View ActivateMaster()
    {
        var r = Root(
            "Почніть працювати майстром",
            "Окремий робочий профіль у вашому акаунті. Перевірка документів не проводиться."
        );
        r.Add(Body("Категорії послуг", 14));
        foreach (var category in _viewModel.Categories.Take(3))
            r.Add(SelectionRow(category, _viewModel.Category == category, () =>
                {
                    _viewModel.Category = category;
                    _viewModel.NotifyAll();
                }));
        r.Add(
            LabeledField(
                "Район роботи",
                _viewModel.MasterDistrict,
                "Київ · Подільський район",
                null,
                v => _viewModel.MasterDistrict = v
            )
        );
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
        portfolio.Add(CreateButton("Додати фото", false, async () =>
            {
                var files = await MediaPicker.Default.PickPhotosAsync();
                var file = files.FirstOrDefault();
                if (file != null)
                {
                    _viewModel.MasterPortfolioPhotoPath = file.FullPath;
                    _viewModel.NotifyAll();
                }
            }));
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
        r.Add(CreateButton("Активувати профіль майстра", true, () =>
            {
                _viewModel.IsMasterProfileActive = true;
                _viewModel.SetMode(AccountMode.Master);
            }));
        return r;
    }

    View Available()
    {
        var r = Root(
            "Робота у вашому районі",
            "Демонстраційні заявки за вашою категорією та районом."
        );
        var filters = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Body("ВАШІ УМОВИ ПОШУКУ", 14, true),
                new Label
                {
                    Text = _viewModel.Category + " · " + _viewModel.MasterDistrict,
                    FontFamily = "Manrope",
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ThemeColor("#202823", "#F4F7FB"),
                },
                Body("Київ · список без мапи та геолокації", 14, true),
                CreateButton("Змінити умови", false, () =>
                    _viewModel.CurrentScreen = "MasterActivation"
                ),
            },
        };
        r.Add(Card(filters));
        var request = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Border
                {
                    BackgroundColor = Color.FromArgb(IsDarkTheme ? "#24384B" : "#EBF3FA"),
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 10 },
                    Padding = new Thickness(10, 6),
                    Content = Body("Нова заявка", 14, true),
                },
                new Label
                {
                    Text = "Заміна змішувача",
                    FontFamily = "Manrope",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ThemeColor("#202823", "#F4F7FB"),
                },
                Body("№2048 · " + _viewModel.Category, 14, true),
                Body(_viewModel.Description),
                Body(_viewModel.District),
                Body("Час обговорюється після вибору", 14, true),
                Body("Бюджет до 1 500 грн", 14),
                CreateButton("Надіслати пропозицію", true, () =>
                    _viewModel.CurrentScreen = "MasterRequest"
                ),
            },
        };
        r.Add(Card(request));
        var second = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Body("НОВА ЗАЯВКА №2051", 14, true),
                new Label
                {
                    Text = "Заміна сифона",
                    FontFamily = "Manrope",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ThemeColor("#202823", "#F4F7FB"),
                },
                Body(_viewModel.MasterDistrict + " · підтікає під мийкою."),
                Body("Бюджет клієнта: до 900 грн", 14, true),
                CreateButton("Переглянути заявку", false, () =>
                    _viewModel.CurrentScreen = "MasterRequest"
                ),
            },
        };
        r.Add(Card(second));
        r.Add(Body("Точна адреса з’явиться тільки якщо клієнт обере вас.", 14, true));
        return r;
    }

    View MasterRequest()
    {
        var r = Root("Заявка №2048", "Перегляньте деталі перед тим, як надіслати пропозицію.");
        r.Add(
            Card(
                new VerticalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        Body("НОВА ЗАЯВКА", 14, true),
                        new Label
                        {
                            Text = "Заміна змішувача",
                            FontFamily = "Manrope",
                            FontSize = 20,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = ThemeColor("#202823", "#F4F7FB"),
                        },
                        Body("Категорія: " + _viewModel.Category, 14),
                        Body("Проблема: " + _viewModel.Description, 14),
                        Body("Район: " + _viewModel.District, 14),
                        Body("Адреса доступна після вибору майстра", 14, true),
                        Body("Бюджет клієнта — до 1 500 грн", 14),
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
                            Text = "Збережена техніка",
                            FontFamily = "Manrope",
                            FontSize = 16,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = ThemeColor("#202823", "#F4F7FB"),
                        },
                        Body(
                            _viewModel.Equipment.FirstOrDefault()?.Name
                                ?? "Клієнт не додав техніку",
                            14
                        ),
                    },
                }
            )
        );
        r.Add(
            CreateButton("Надіслати пропозицію", true, () => _viewModel.CurrentScreen = "Proposal")
        );
        r.Add(
            CreateButton("Назад до заявок", false, () =>
                _viewModel.CurrentScreen = "AvailableRequests"
            )
        );
        return r;
    }

    View Proposal()
    {
        var r = Root(
            "Ваша відповідь клієнту",
            "Заявка №2048 · напишіть коротко й по суті. Часові слоти не потрібні."
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
                    _viewModel.ProposalSent = true;
                    _viewModel.CurrentScreen = "ProposalSent";
                }
            }));
        r.Add(CreateButton("Зберегти як чернетку", false, () =>
            {
                _viewModel.ProposalSent = false;
                _viewModel.CurrentScreen = "AvailableRequests";
            }));
        return r;
    }

    View ProposalSentScreen()
    {
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
                            Text = "Заміна змішувача",
                            FontFamily = "Manrope",
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = ThemeColor("#202823", "#F4F7FB"),
                        },
                        Body("Орієнтовна ціна: " + _viewModel.MasterPrice + " грн"),
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

    View MasterCompleted()
    {
        var r = Root(
            "Роботу завершено",
            "Клієнт позначив заявку завершеною. Відгук з’явиться у вашому профілі."
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
                        Body("ЗАВЕРШЕНО", 14, true),
                        new Label
                        {
                            Text = "Заміна змішувача",
                            FontFamily = "Manrope",
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = ThemeColor("#202823", "#F4F7FB"),
                        },
                        Body(
                            "Ваша орієнтовна ціна: "
                                + _viewModel.MasterPrice
                                + " грн. Оцінка клієнта: 5 із 5 · демонстраційний відгук."
                        ),
                    },
                },
            }
        );
        r.Add(
            CreateButton("До доступних заявок", true, () =>
                _viewModel.CurrentScreen = "AvailableRequests"
            )
        );
        r.Add(
            CreateButton("Переглянути відгук", false, () => _viewModel.CurrentScreen = "Profile")
        );
        return r;
    }
}
