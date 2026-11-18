using Microsoft.Maui.Controls.Shapes;

namespace BudApka;

public partial class MainPage
{
    View Visit()
    {
        var r = Root(
            "Запишіть обговорений час",
            "Це зручна нотатка після чату, не формальний контракт."
        );
        r.Add(Body("Дата", 14));
        var datePicker = new DatePicker
        {
            Date = _viewModel.VisitDate,
            MinimumDate = DateTime.Today,
            FontFamily = "Inter",
            FontSize = 16,
            TextColor = ThemeColor("#202823", "#F4F7FB"),
            BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
        };
        datePicker.DateSelected += (_, e) => _viewModel.VisitDate = e.NewDate ?? DateTime.Today;
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
                _viewModel.VisitWindow,
                "Після 17:00",
                "Довільний текст — без фіксованих слотів.",
                v => _viewModel.VisitWindow = v,
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
                    Text = "Заміна змішувача",
                    FontFamily = "Manrope",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ThemeColor("#202823", "#F4F7FB"),
                },
                Body(_viewModel.SelectedMaster?.Name ?? "Олександр Коваль"),
                Body(_viewModel.OrderAddress),
                Body("Орієнтир: " + (_viewModel.SelectedMaster?.Price ?? "1 200 грн")),
            },
        };
        r.Add(Card(details));
        r.Add(CreateButton("Зберегти деталі візиту", true, () =>
            {
                if (ValidateFields())
                {
                    _viewModel.RecordVisit();
                }
            }));
        r.Add(CreateButton("Повернутися до чату", false, () => _viewModel.CurrentScreen = "Chat"));
        return r;
    }

    View Completion()
    {
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
                            (_viewModel.SelectedMaster?.Name ?? "Олександр Коваль")
                                + " · заявка №2048"
                        ),
                    },
                },
            }
        );
        r.Add(
            LabeledField(
                "Оцінка",
                _viewModel.Rating.ToString(),
                "5 із 5",
                "Оберіть оцінку від 1 до 5.",
                v =>
                {
                    if (int.TryParse(v, out var value))
                        _viewModel.Rating = Math.Clamp(value, 1, 5);
                },
                Keyboard.Numeric
            )
        );
        r.Add(Body("Відгук · необов’язково", 14));
        var review = new Editor
        {
            Text = _viewModel.ReviewText,
            Placeholder = "Напишіть кілька слів про роботу",
            FontFamily = "Inter",
            FontSize = 16,
            TextColor = ThemeColor("#202823", "#F4F7FB"),
            PlaceholderColor = ThemeColor("#566257", "#BFCADA"),
            BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
            HeightRequest = 112,
        };
        review.TextChanged += (_, e) => _viewModel.ReviewText = e.NewTextValue;
        r.Add(review);
        r.Add(CreateButton("Повернути попередній статус", false, () =>
        {
            _viewModel.Status = OrderStatus.VisitRecorded;
            _viewModel.CurrentScreen = "Orders";
        }));
        r.Add(CreateButton("Зберегти відгук", true, () =>
            {
                _viewModel.Status = OrderStatus.Completed;
                _viewModel.CurrentScreen = "Orders";
            }));
        r.Add(CreateButton("Пропустити відгук", false, () =>
            {
                _viewModel.Status = OrderStatus.Completed;
                _viewModel.CurrentScreen = "Orders";
            }));
        return r;
    }

    View Orders()
    {
        if (_viewModel.Mode == AccountMode.Master)
            return MasterJobs();
        var r = Root("Мої замовлення", "Поточний статус і наступна дія — в одному місці.");
        if (_viewModel.HasOrder)
        {
            var current = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    Body(_viewModel.StatusText, 14),
                    new Label
                    {
                        Text = _viewModel.OrderCategory,
                        FontFamily = "Manrope",
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = ThemeColor("#202823", "#F4F7FB"),
                    },
                    Body(_viewModel.OrderDistrict),
                    Body(_viewModel.SelectedMaster?.Name ?? "Майстра ще не обрано"),

                },
            };
            if (!string.IsNullOrEmpty(_viewModel.RecordedVisitText))
            {
                current.Add(Body(_viewModel.RecordedVisitText, 14, true));
            }
            r.Add(Card(current));
            if (_viewModel.Status == OrderStatus.HasResponses)
            {
                r.Add(CreateButton("Переглянути відповіді", true, () =>
                    _viewModel.CurrentScreen = "Responses"));
            }
            if (_viewModel.CanChat)
            {
                r.Add(CreateButton("Відкрити чат", false, () =>
                    _viewModel.CurrentScreen = "Chat"));
            }
            if (_viewModel.Status == OrderStatus.VisitRecorded)
            {
                r.Add(CreateAsyncButton("Позначити завершеним", true, () =>
                    ChangeOrderStatusAsync(OrderStatus.Completed)));
            }
            if (_viewModel.Status is not (OrderStatus.Completed or OrderStatus.Cancelled))
            {
                r.Add(CreateAsyncButton("Скасувати замовлення", false, () =>
                    ChangeOrderStatusAsync(OrderStatus.Cancelled)));
            }
            var done = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    Body("ЗАВЕРШЕНО", 14, true),
                    new Label
                    {
                        Text = "Підключення пральної машини",
                        FontFamily = "Manrope",
                        FontSize = 18,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = ThemeColor("#202823", "#F4F7FB"),
                    },
                    Body("Демонстраційне попереднє замовлення."),
                    Body("Відгук залишено · оцінка 5 із 5", 14, true),
                },
            };
            r.Add(Card(done));
            r.Add(
                Body(
                    string.Join(
                        " ",
                        "Статуси: надіслано → є відповіді → майстра обрано →",
                        "час записано → завершено / скасовано."
                    ),
                    14,
                    true
                )
            );
        }
        else
        {
            r.Add(
                Card(
                    new VerticalStackLayout
                    {
                        Spacing = 8,
                        Children =
                        {
                            new Label
                            {
                                Text = "Поки немає замовлень",
                                FontFamily = "Manrope",
                                FontSize = 18,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = ThemeColor("#202823", "#F4F7FB"),
                            },
                            Body("Створіть заявку, щоб знайти майстра."),
                        },
                    }
                )
            );
            r.Add(
                CreateButton("Створити заявку", true, () => _viewModel.CurrentScreen = "NewRequest")
            );
        }
        return r;
    }

    async Task ChangeOrderStatusAsync(OrderStatus next)
    {
        OrderStatus previous = _viewModel.Status;
        bool completing = next == OrderStatus.Completed;
        bool confirmed = await DisplayAlertAsync(
            completing ? "Завершити замовлення?" : "Скасувати замовлення?",
            completing ? "Замовлення буде позначено завершеним."
                : "Замовлення буде скасовано, а активний чат закриється.",
            completing ? "Завершити" : "Скасувати замовлення",
            "Повернутися"
        );

        if (!confirmed || _viewModel.Status != previous || _viewModel.Mode != AccountMode.Client)
        {
            return;
        }

        _viewModel.Status = next;
        SemanticScreenReader.Default.Announce(_viewModel.StatusText);
        if (completing)
        {
            _viewModel.CurrentScreen = "Completion";
        }
        else
        {
            _viewModel.NotifyAll();
        }
    }

    View MasterJobs()
    {
        var r = Root("Мої роботи", "Замовлення, у яких клієнт обрав вас.");
        if (_viewModel.SelectedMaster == null)
        {
            r.Add(Card(Body("Поки немає робіт, для яких клієнт обрав майстра.")));
            r.Add(CreateButton("До доступних заявок", true, () =>
                _viewModel.CurrentScreen = "AvailableRequests"));
            return r;
        }

        var current = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Body(_viewModel.StatusText, 14, true),
                new Label
                {
                    Text = "Заміна змішувача",
                    FontFamily = "Manrope",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ThemeColor("#202823", "#F4F7FB"),
                },
                Body(_viewModel.OrderDistrict),
            },
        };
        if (!string.IsNullOrEmpty(_viewModel.RecordedVisitText))
        {
            current.Add(Body(_viewModel.RecordedVisitText));
        }
        r.Add(Card(current));
        if (_viewModel.CanChat)
        {
            r.Add(CreateButton("Відкрити чат", true, () => _viewModel.CurrentScreen = "Chat"));
        }
        var done = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Body("ЗАВЕРШЕНО", 14, true),
                new Label
                {
                    Text = "Підключення пральної машини",
                    FontFamily = "Manrope",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ThemeColor("#202823", "#F4F7FB"),
                },
                Body("Демонстраційне замовлення."),
                Body("Відгук залишено · оцінка 5 із 5", 14, true),
            },
        };
        r.Add(Card(done));
        r.Add(Body("Статус змінюється після підтвердження клієнтом.", 14, true));
        return r;
    }
}
