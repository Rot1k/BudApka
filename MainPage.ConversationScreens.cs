using Microsoft.Maui.Controls.Shapes;

namespace BudApka;

public partial class MainPage
{
    View MasterProfile()
    {
        var master = _viewModel.PreviewedMaster ?? _viewModel.Offers[0];
        var r = Root(
            "Познайомтеся з майстром",
            "Профіль, досвід і відгуки перед вибором. Усі дані демонстраційні."
        );
        var card = new VerticalStackLayout { Spacing = 8 };
        card.Add(
            new Border
            {
                WidthRequest = 68,
                HeightRequest = 68,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Content = new Image { Source = master.PhotoAsset, Aspect = Aspect.AspectFill },
            }
        );
        card.Add(
            new Label
            {
                Text = master.Name,
                FontFamily = "Manrope",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = ThemeColor("#202823", "#F4F7FB"),
            }
        );
        card.Add(Body(master.Specialty + " · Київ", 14));
        card.Add(
            new Border
            {
                BackgroundColor = Color.FromArgb(IsDarkTheme ? "#244D38" : "#EAF4ED"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(8, 6),
                Content = Body("Є відгуки", 14),
            }
        );
        card.Add(
            Body("★ " + master.Rating + " · " + master.Reviews + " відгуків · приклад", 14, true)
        );
        card.Add(
            new Label
            {
                Text = "Про майстра",
                FontFamily = "Manrope",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = ThemeColor("#202823", "#F4F7FB"),
            }
        );
        card.Add(
            Body(
                "Допомагаю з ремонтом сантехніки: змішувачі, сифони, "
                    + "підключення пральних машин. Приїжджаю вчасно, "
                    + "погоджую вартість до початку робіт.",
                13
            )
        );
        card.Add(
            new Label
            {
                Text = "Послуги",
                FontFamily = "Manrope",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = ThemeColor("#202823", "#F4F7FB"),
            }
        );
        card.Add(
            Body(
                string.Join(
                    Environment.NewLine,
                    "Заміна змішувача — від 900 грн",
                    "Підключення техніки — від 700 грн",
                    "Діагностика та виїзд — від 300 грн"
                ),
                12,
                true
            )
        );
        card.Add(
            new Label
            {
                Text = "Відгуки клієнтів",
                FontFamily = "Manrope",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = ThemeColor("#202823", "#F4F7FB"),
            }
        );
        card.Add(
            Body(
                string.Join(
                    " ",
                    "«Швидко знайшов проблему, усе пояснив та залишив кухню чистою». ",
                    "· Марія · ★ 5,0 · 12 листопада"
                ),
                13
            )
        );
        r.Add(Card(card));
        r.Add(
            new Label
            {
                Text = "Приклад роботи · заміна змішувача",
                FontFamily = "Inter",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = ThemeColor("#202823", "#F4F7FB"),
            }
        );
        r.Add(
            new Image
            {
                Source = "mixer_example.jpg",
                HeightRequest = 180,
                Aspect = Aspect.AspectFill,
            }
        );
        r.Add(
            CreateAsyncButton("Обрати " + master.Name.Split(' ')[0], true, () =>
                ConfirmMasterAsync(master)
            )
        );
        r.Add(Body("Після вибору майстер отримає точну адресу, а ви — доступ до чату.", 14, true));
        r.Add(CreateButton("До відповідей майстрів", false, () =>
            _viewModel.CurrentScreen = "Responses"));
        return r;
    }

    async Task ConfirmMasterAsync(MasterOffer master)
    {
        bool confirmed = await DisplayAlertAsync(
            $"Обрати майстра: {master.Name}?",
            "Точна адреса стане доступною цьому майстру після підтвердження.",
            "Обрати майстра",
            "Повернутися");
        if (confirmed)
        {
            _viewModel.ChooseMaster(master);
        }
    }

    View Chat()
    {
        if (!_viewModel.CanChat)
        {
            var empty = Root("Чати", "Активних розмов поки немає.");
            bool client = _viewModel.Mode == AccountMode.Client;
            empty.Add(Body(client
                ? "Чат відкриється після вибору майстра."
                : "Чат відкриється, коли клієнт обере майстра."));
            string destination = client ? "Orders" : "AvailableRequests";
            empty.Add(CreateButton(client ? "До замовлень" : "До заявок", true, () =>
                _viewModel.CurrentScreen = destination));
            return empty;
        }
        var r = Root(
            _viewModel.Mode == AccountMode.Master
                ? "Клієнт Олег"
                : _viewModel.SelectedMaster?.Name ?? "Олександр Коваль",
            _viewModel.Mode == AccountMode.Master
                ? "Клієнт обрав вас · заявка №2048. Тепер доступні чат і точна адреса."
                : "Майстра обрано · заявка №2048. Обговоріть деталі вільним текстом."
        );
        r.Add(
            Card(
                new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        Body(
                            _viewModel.Mode == AccountMode.Master
                                ? "КЛІЄНТ ОБРАВ ВАС"
                                : "ОБРАНИЙ МАЙСТЕР",
                            12,
                            true
                        ),
                        new Label
                        {
                            Text = _viewModel.OrderCategory,
                            FontFamily = "Manrope",
                            FontSize = 18,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = ThemeColor("#202823", "#F4F7FB"),
                        },
                        Body(
                            string.Join(
                                Environment.NewLine,
                                "Орієнтир майстра: "
                                    + (_viewModel.SelectedMaster?.Price ?? "1 200 грн"),
                                "Точна адреса: " + _viewModel.OrderAddress
                            ),
                            14
                        ),
                    },
                }
            )
        );
        r.Add(Body("Демо-чат · повідомлення зберігаються в цій сесії.", 14, true));
        var messages = new VerticalStackLayout { Spacing = 12 };
        foreach (var message in _viewModel.Messages)
        {
            messages.Add(ChatBubble(message));
        }
        r.Add(messages);

        r.Add(Body("Повідомлення", 14));
        var entry = Field("Напишіть повідомлення", v => _viewModel.MessageDraft = v);
        entry.Text = _viewModel.MessageDraft;
        SemanticProperties.SetDescription(entry, "Текст повідомлення");
        var send = ButtonStyle("Додати повідомлення", false);
        send.IsEnabled = !string.IsNullOrWhiteSpace(entry.Text);
        var feedback = Body("", 14, true);
        feedback.IsVisible = false;
        entry.TextChanged += (_, e) =>
            send.IsEnabled = !string.IsNullOrWhiteSpace(e.NewTextValue);
        BindButtonAction(send, () =>
        {
            if (_viewModel.AddChatMessage())
            {
                messages.Add(ChatBubble(_viewModel.Messages[^1]));
                entry.Text = "";
                feedback.Text = "Додано до демо-чату.";
                feedback.IsVisible = true;
                SemanticScreenReader.Default.Announce(feedback.Text);
            }
        });
        r.Add(entry);
        r.Add(send);
        r.Add(feedback);

        if (_viewModel.Mode == AccountMode.Client)
        {
            r.Add(CreateButton("Час візиту", true, () =>
                _viewModel.CurrentScreen = "VisitDetails"));
        }
        else if (_viewModel.Status == OrderStatus.VisitRecorded)
        {
            r.Add(Body("Час візиту: " + _viewModel.RecordedVisitText, 14));
        }

        return r;
    }

    View ChatBubble(ChatMessage message)
    {
        bool mine = message.FromClient == (_viewModel.Mode == AccountMode.Client);
        var bubble = new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                Body(message.Text),
                Body((mine ? "Ви" : "Співрозмовник") + " · " + message.Time, 14, true),
            },
        };
        return new Border
        {
            BackgroundColor = ThemeColor(
                mine ? "#FFF5D6" : "#FFFFFF", mine ? "#3B3423" : "#202833"),
            Stroke = ThemeColor("#E3E7DF", "#344151"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Padding = 12,
            Margin = mine ? new Thickness(24, 0, 0, 0) : new Thickness(0, 0, 24, 0),
            Content = bubble,
            HorizontalOptions = mine ? LayoutOptions.End : LayoutOptions.Start,
        };
    }
}
