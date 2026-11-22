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
        card.Add(Body(master.Name, 20));
        card.Add(Body(master.Specialty, 14));
        if (master.Id == PrototypeViewModel.LocalMasterId)
        {
            if (!string.IsNullOrWhiteSpace(_viewModel.MasterAbout))
            {
                card.Add(Body(_viewModel.MasterAbout));
            }
            if (_viewModel.MasterPortfolioPhotoPath.Length > 0)
            {
                card.Add(new Image
                {
                    Source = ImageSource.FromFile(_viewModel.MasterPortfolioPhotoPath),
                    HeightRequest = 180,
                    Aspect = Aspect.AspectFit,
                });
            }
        }
        else
        {
            card.Add(new Image
            {
                Source = master.PhotoAsset,
                HeightRequest = 90,
                Aspect = Aspect.AspectFit,
            });
            card.Add(WithIcon("star", Body($"{master.Rating} · {master.Reviews} відгуків", 14, true)));
            card.Add(Body($"Досвід · {master.Years} років", 14));
        }
        card.Add(Body(master.Message));
        card.Add(Body(master.Price + " · довідкова ціна"));
        r.Add(Card(card));
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
        send.ImageSource = Icon("send");
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
                _viewModel.CurrentScreen = "VisitDetails", "calendar"));
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
