using Microsoft.Maui.Controls.Shapes;

namespace BudApka;

public partial class MainPage : ContentPage
{
    static readonly Dictionary<(string Light, string Dark), string> _themeColorTokens = new()
    {
        [("#FAFAF7", "#141A22")] = "CanvasLight",
        [("#FFFFFF", "#202833")] = "CardLight",
        [("#F1F2ED", "#2A3542")] = "SubtleLight",
        [("#202823", "#F4F7FB")] = "TextPrimary",
        [("#566257", "#BFCADA")] = "TextSecondary",
        [("#727D72", "#91A1B7")] = "BorderDefault",
        [("#E3E7DF", "#344151")] = "BorderSubtle",
        [("#FFB800", "#E3A600")] = "ActionPrimary",
        [("#FF9800", "#F4BB32")] = "ActionPressed",
    };
    readonly PrototypeViewModel _viewModel = new();
    string? _lastRenderedScreen;
    bool IsDarkTheme => Application.Current?.UserAppTheme == AppTheme.Dark;

    Color ThemeColor(string light, string dark)
    {
        if (_themeColorTokens.TryGetValue((light, dark), out string? token)
            && Application.Current?.Resources.TryGetValue(
                IsDarkTheme ? token.Replace("Light", "") + "Dark" : token, out var value) == true
            && value is Color resourceColor)
        {
            return resourceColor;
        }

        return Color.FromArgb(IsDarkTheme ? dark : light);
    }

    public MainPage()
    {
        InitializeComponent();
        BindingContext = _viewModel;
        _viewModel.PropertyChanged += (_, _) => MainThread.BeginInvokeOnMainThread(Render);
        if (Application.Current != null)
            Application.Current.RequestedThemeChanged += (_, _) => Render();
        Render();
    }

    void Render()
    {
        string screen = _viewModel.CurrentScreen;
        bool screenChanged = _lastRenderedScreen != screen;
        if (screenChanged)
        {
            TrackNavigation(screen);
            _lastRenderedScreen = screen;
        }
        HeaderHost.MaximumWidthRequest = 390;
        BodyHost.MaximumWidthRequest = 390;
        HeaderHost.HorizontalOptions = LayoutOptions.Center;
        BodyHost.HorizontalOptions = LayoutOptions.Center;
        BackgroundColor = ThemeColor("#FAFAF7", "#141A22");
        _fieldValidators.Clear();
        HeaderHost.Content = Header();
        BodyHost.Content = new ScrollView
        {
            Content = Screen(),
            BackgroundColor = ThemeColor("#FAFAF7", "#141A22"),
            VerticalScrollBarVisibility = ScrollBarVisibility.Never,
        };
        NavigationHost.Content = Footer();

        if (screenChanged)
        {
            BodyHost.Opacity = 0;
            BodyHost.TranslationY = 10;
            _ = Task.WhenAll(
                BodyHost.FadeToAsync(1, 180, Easing.CubicOut),
                BodyHost.TranslateToAsync(0, 0, 180, Easing.CubicOut)
            );
        }
    }

    View Header()
    {
        var title = new Label
        {
            Text = "БУД Апка",
            FontFamily = "Manrope",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = ThemeColor("#202823", "#F4F7FB"),
        };
        var sub = new Label
        {
            Text =
                _viewModel.Mode == AccountMode.Client
                    ? "Клієнт · демонстраційні дані"
                    : "Майстер · демонстраційні дані",
            FontFamily = "Inter",
            FontSize = 14,
            TextColor = ThemeColor("#566257", "#BFCADA"),
        };
        var g = new Grid
        {
            Padding = new Thickness(16),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            },
            BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
        };
        if (CanGoBack)
        {
            var back = new Button
            {
                Text = "←",
                FontSize = 24,
                WidthRequest = 48,
                HeightRequest = 48,
                Padding = 0,
                Margin = new Thickness(0, 0, 8, 0),
                CornerRadius = 12,
                BackgroundColor = ThemeColor("#F1F2ED", "#2A3542"),
                TextColor = ThemeColor("#202823", "#F4F7FB"),
                VerticalOptions = LayoutOptions.Center,
            };
            SemanticProperties.SetDescription(back, "Назад");
            BindButtonAction(back, () => GoBack());
            g.Add(back, 0, 0);
        }
        g.Add(new VerticalStackLayout { Spacing = 2, Children = { title, sub } }, 1, 0);
        var actions = new HorizontalStackLayout
        {
            Spacing = 4,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End,
        };
        var themeButton = new Button
        {
            Text = IsDarkTheme ? "☀" : "☾",
            FontFamily = "Inter",
            FontSize = 21,
            WidthRequest = 44,
            HeightRequest = 44,
            Padding = 0,
            CornerRadius = 12,
            BackgroundColor = ThemeColor("#F1F2ED", "#2A3542"),
            TextColor = ThemeColor("#202823", "#F4F7FB"),
            BorderWidth = 0,
        };
        Microsoft.Maui.Controls.SemanticProperties.SetDescription(
            themeButton,
            IsDarkTheme ? "Світла тема" : "Темна тема"
        );
        themeButton.Clicked += (_, _) => ToggleTheme();
        actions.Add(themeButton);
        actions.Add(
            new ImageButton
            {
                Source = IsDarkTheme ? "dark_icon_bell.png" : "icon_bell.png",
                WidthRequest = 44,
                HeightRequest = 44,
                BackgroundColor = Colors.Transparent,
                Padding = 12,
                Aspect = Aspect.AspectFit,
                Command = new Command(() =>
                    DisplayAlertAsync("Повідомлення", "Нових повідомлень поки немає.", "OK")
                ),
            }
        );
        g.Add(actions, 2, 0);
        return g;
    }

    void ToggleTheme()
    {
        var app = Application.Current;
        if (app == null)
            return;
        var next = IsDarkTheme ? AppTheme.Light : AppTheme.Dark;
        app.UserAppTheme = next;
        Microsoft.Maui.Storage.Preferences.Default.Set(
            "budapka-theme",
            next == AppTheme.Dark ? "dark" : "light"
        );
        Render();
    }

    VerticalStackLayout Root(string title, string? subtitle = null)
    {
        var r = new VerticalStackLayout { Padding = new Thickness(20, 20, 20, 24), Spacing = 16 };
        r.Add(ModeSwitch());
        r.Add(
            new Label
            {
                Text = title,
                FontFamily = "Manrope",
                FontSize = 28,
                FontAttributes = FontAttributes.Bold,
                LineHeight = 1.28,
                TextColor = ThemeColor("#202823", "#F4F7FB"),
            }
        );
        if (subtitle != null)
            r.Add(
                new Label
                {
                    Text = subtitle,
                    FontFamily = "Inter",
                    FontSize = 16,
                    LineHeight = 1.5,
                    TextColor = ThemeColor("#566257", "#BFCADA"),
                }
            );
        return r;
    }

    View ModeSwitch()
    {
        var g = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
            },
            ColumnSpacing = 8,
        };
        g.Add(ModeButton("Замовити", AccountMode.Client), 0, 0);
        g.Add(ModeButton("Працювати", AccountMode.Master), 1, 0);
        return g;
    }

    Button ModeButton(string text, AccountMode mode)
    {
        bool selected = _viewModel.Mode == mode;
        var b = ButtonStyle(text, selected);
        b.HeightRequest = 52;
        BindButtonAction(b, () => _viewModel.SetMode(mode));
        return b;
    }

    Button ButtonStyle(string text, bool primary)
    {
        var b = new Button
        {
            Text = text,
            FontFamily = "Inter",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            HeightRequest = 52,
            Padding = new Thickness(16, 0),
            BackgroundColor = primary
                ? ThemeColor("#FFB800", "#E3A600")
                : ThemeColor("#FFFFFF", "#202833"),
            TextColor = primary
                ? ThemeColor("#202823", "#202823")
                : ThemeColor("#202823", "#F4F7FB"),
            BorderColor = primary ? Colors.Transparent : ThemeColor("#727D72", "#91A1B7"),
            BorderWidth = primary ? 0 : 1,
            Shadow = new Shadow
            {
                Brush = Color.FromArgb(primary ? "#26000000" : "#12000000"),
                Offset = new Point(0, 2),
                Radius = primary ? 8 : 3,
                Opacity = 0.18f,
            },
        };
        return b;
    }

    Label Body(string text, double size = 16, bool secondary = false) =>
        new()
        {
            Text = text,
            FontFamily = "Inter",
            FontSize = size,
            LineHeight = 1.5,
            TextColor = secondary
                ? ThemeColor("#566257", "#BFCADA")
                : ThemeColor("#202823", "#F4F7FB"),
        };

    View Card(View content)
    {
        return new Border
        {
            BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
            Stroke = ThemeColor("#E3E7DF", "#344151"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            Padding = 16,
            Shadow = new Shadow
            {
                Brush = Color.FromArgb("#18000000"),
                Offset = new Point(0, 2),
                Radius = 8,
                Opacity = 0.12f,
            },
            Content = content,
        };
    }

    View Footer() =>
        _viewModel.CurrentScreen switch
        {
            "NewRequest" => WorkflowFooter("Далі · адреса", () =>
                _viewModel.CurrentScreen = "Address"
            ),
            "Address" => WorkflowFooter("Перевірити заявку", () =>
            {
                if (_validateAddressSelection?.Invoke() == true && ValidateFields())
                {
                    _viewModel.CurrentScreen = "ReviewRequest";
                }
            }),
            "ReviewRequest" or "SignIn" or "MasterActivation" => new ContentView
            {
                HeightRequest = 0,
                IsVisible = false,
            },
            _ => BottomNav(),
        };

    View WorkflowFooter(string text, Action action)
    {
        var grid = new Grid
        {
            Padding = new Thickness(14, 8),
            HeightRequest = 72,
            BackgroundColor = ThemeColor("#FAFAF7", "#141A22"),
        };
        var button = ButtonStyle(text, true);
        BindButtonAction(button, action);
        grid.Add(button, 0, 0);
        return grid;
    }

    View BottomNav()
    {
        bool master = _viewModel.Mode == AccountMode.Master;
        string[] labels = master
            ? new[] { "Доступні", "Мої роботи", "Чати", "Профіль" }
            : new[] { "Головна", "Заявки", "Чати", "Профіль" };
        string[] icons = { "icon_home.png", "icon_request.png", "icon_chat.png", "icon_user.png" };
        string active = _viewModel.CurrentScreen switch
        {
            "Home" => master ? "Доступні" : "Головна",
            "Chat" => "Чати",
            "Profile" or "Equipment" or "MasterActivation" => "Профіль",
            "Orders" => master ? "Мої роботи" : "Заявки",
            "AvailableRequests" or "MasterRequest" => master ? "Доступні" : "Заявки",
            "Proposal" or "ProposalSent" or "MasterCompleted" => master ? "Мої роботи" : "Заявки",
            _ => "Заявки",
        };
        Action[] actions = new Action[]
        {
            () => NavigateTab(ModeHome),
            () => NavigateTab("Orders"),
            () => NavigateTab("Chat"),
            () => NavigateTab("Profile"),
        };
        var g = new Grid
        {
            Padding = new Thickness(4, 6, 4, 8),
            ColumnSpacing = 2,
            HorizontalOptions = LayoutOptions.Fill,
            MinimumHeightRequest = 86,
            RowDefinitions = { new RowDefinition(GridLength.Auto) },
            BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
            },
        };
        for (int i = 0; i < 4; i++)
        {
            int n = i;
            bool selected = labels[i] == active;
            var stack = new VerticalStackLayout
            {
                Spacing = 2,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                Padding = new Thickness(2, 6),
                MinimumHeightRequest = 60,
                BackgroundColor = Colors.Transparent,
            };
            stack.Add(
                new Image
                {
                    Source = IsDarkTheme && !selected ? $"dark_{icons[i]}" : icons[i],
                    WidthRequest = 24,
                    HeightRequest = 24,
                    HorizontalOptions = LayoutOptions.Center,
                }
            );
            stack.Add(
                new Label
                {
                    Text = labels[i],
                    FontFamily = "Inter",
                    FontSize = 14,
                    LineBreakMode = LineBreakMode.WordWrap,
                    HorizontalOptions = LayoutOptions.Fill,
                    HorizontalTextAlignment = TextAlignment.Center,
                    TextColor = selected
                        ? ThemeColor("#202823", "#202823")
                        : ThemeColor("#202823", "#F4F7FB"),
                }
            );
            var tap = new TapGestureRecognizer();
            SemanticProperties.SetDescription(stack, selected ? $"{labels[i]}, вибрано" : labels[i]);
            tap.Tapped += (_, _) => actions[n]();
            stack.GestureRecognizers.Add(tap);
            g.Add(
                new Border
                {
                    HorizontalOptions = LayoutOptions.Fill,
                    BackgroundColor = selected
                        ? ThemeColor("#FFB800", "#E3A600")
                        : Colors.Transparent,
                    StrokeThickness = 0,
                    StrokeShape = new RoundRectangle { CornerRadius = 12 },
                    Content = stack,
                },
                i,
                0
            );
        }
        return new Border
        {
            BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
            Stroke = ThemeColor("#E3E7DF", "#344151"),
            StrokeThickness = 1,
            Content = g,
            HorizontalOptions = LayoutOptions.Fill,
            MinimumHeightRequest = 86,
        };
    }


}
