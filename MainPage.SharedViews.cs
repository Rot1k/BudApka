using Microsoft.Maui.Controls.Shapes;

namespace BudApka;

public partial class MainPage
{
    readonly List<(Entry Entry, Func<bool> Validate)> _fieldValidators = new();

    bool ValidateFields()
    {
        Entry? firstInvalid = null;
        foreach (var field in _fieldValidators)
        {
            if (!field.Validate())
            {
                firstInvalid ??= field.Entry;
            }
        }

        if (firstInvalid == null)
        {
            return true;
        }

        if (BodyHost.Content is ScrollView scroll)
        {
            _ = scroll.ScrollToAsync(firstInvalid, ScrollToPosition.Center, false);
        }

        firstInvalid.Focus();
        return false;
    }

    static string? Required(string value, string message) =>
        string.IsNullOrWhiteSpace(value) ? message : null;

    static string? ValidateAmount(string value)
    {
        string digits = string.Concat(value.Where(c => !char.IsWhiteSpace(c)));
        return int.TryParse(digits, out int amount) && amount > 0
            ? null
            : "Введіть суму в гривнях цілим числом більше нуля.";
    }

    View WithIcon(string icon, View content, bool onAccent = false)
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
        row.Add(new Image
        {
            Source = Icon(icon, onAccent),
            WidthRequest = 24,
            HeightRequest = 24,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
        });
        row.Add(content, 1, 0);
        return row;
    }

    View SelectionRow(string title, bool selected, Action action, string? icon = null)
    {
        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            },
            ColumnSpacing = 12,
        };
        var label = new Label
        {
            Text = title,
            FontFamily = "Inter",
            FontSize = 15,
            TextColor = ThemeColor("#202823", "#F4F7FB"),
            VerticalOptions = LayoutOptions.Center,
        };
        row.Add(icon == null ? label : WithIcon(icon, label));
        if (selected)
        {
            row.Add(new Image
            {
                Source = Icon("check"),
                WidthRequest = 24,
                HeightRequest = 24,
                VerticalOptions = LayoutOptions.Center,
            }, 1);
        }
        else
        {
            row.Add(Body("○", 20, true), 1);
        }
        var border = new Border
        {
            BackgroundColor = ThemeColor(
                selected ? "#FFF5D6" : "#FFFFFF",
                selected ? "#263140" : "#202833"
            ),
            Stroke = ThemeColor(selected ? "#FFB800" : "#E3E7DF", selected ? "#E3A600" : "#344151"),
            StrokeThickness = selected ? 2 : 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = 14,
            Content = row,
        };
        var tap = new TapGestureRecognizer();
        SemanticProperties.SetDescription(border, selected ? $"{title}, вибрано" : title);
        tap.Tapped += (_, _) => action();
        border.GestureRecognizers.Add(tap);
        return border;
    }

    View LabeledField(
        string label,
        string value,
        string placeholder,
        string? helper,
        Action<string> changed,
        Keyboard? keyboard = null,
        string? suffix = null,
        Func<string, string?>? validate = null,
        Func<char, bool>? allowCharacter = null,
        int maxLength = int.MaxValue,
        Action<Entry>? configureEntry = null,
        bool validateOnUnfocus = true,
        string? prefix = null
    )
    {
        var stack = new VerticalStackLayout { Spacing = 6 };
        if (!string.IsNullOrWhiteSpace(label))
            stack.Add(
                new Label
                {
                    Text = label,
                    FontFamily = "Inter",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = ThemeColor("#202823", "#F4F7FB"),
                }
            );
        var entry = Field(placeholder, changed, keyboard, allowCharacter);
        Color fieldBackground = entry.BackgroundColor;
        entry.BackgroundColor = Colors.Transparent;
#if WINDOWS
        entry.HandlerChanged += (_, _) =>
        {
            if (entry.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox textBox)
            {
                textBox.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);

            }
        };
#endif
        entry.MaxLength = maxLength;
        entry.Text = value;
        configureEntry?.Invoke(entry);
        entry.HorizontalOptions = LayoutOptions.Fill;
        SemanticProperties.SetDescription(entry, prefix == null ? label : $"{label}, {prefix}");
        View input = entry;
        if (prefix != null)
        {
            var prefixRow = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                },
                ColumnSpacing = 8,
            };
            var prefixLabel = Body(prefix);
            prefixLabel.VerticalOptions = LayoutOptions.Center;
            prefixRow.Add(prefixLabel);
            prefixRow.Add(entry, 1);
            input = prefixRow;
        }
        var control = new Border
        {
            BackgroundColor = fieldBackground,
            Stroke = ThemeColor("#727D72", "#91A1B7"),
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(12, 0),
            Content = input,
        };
        Color errorColor = ThemeColor("#B33232", "#FFAAAA");
        var error = Body("", 14);
        error.TextColor = errorColor;
        error.IsVisible = false;
        bool Validate()
        {
            string? message = validate?.Invoke(entry.Text ?? "");
            error.Text = message;
            error.IsVisible = message != null;
            UpdateStroke();
            return message == null;
        }

        void UpdateStroke()
        {
            control.Stroke = error.IsVisible ? errorColor
                : entry.IsFocused ? ThemeColor("#805600", "#F4BB32")
                : ThemeColor("#727D72", "#91A1B7");
            control.StrokeThickness = entry.IsFocused || error.IsVisible ? 2 : 1;
        }

        entry.Focused += (_, _) => UpdateStroke();
        entry.Unfocused += (_, _) =>
        {
            if (validateOnUnfocus)
            {
                Validate();
            }
            else
            {
                UpdateStroke();
            }
        };
        entry.TextChanged += (_, _) =>
        {
            if (error.IsVisible)
            {
                Validate();
            }
        };
        if (validate != null)
        {
            _fieldValidators.Add((entry, Validate));
        }
        if (string.IsNullOrWhiteSpace(suffix))
            stack.Add(control);
        else
        {
            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                },
                ColumnSpacing = 8,
            };
            grid.Add(control);
            grid.Add(
                new Label
                {
                    Text = suffix,
                    FontFamily = "Inter",
                    FontSize = 15,
                    TextColor = ThemeColor("#566257", "#BFCADA"),
                    VerticalOptions = LayoutOptions.Center,
                },
                1
            );
            stack.Add(grid);
        }
        if (!string.IsNullOrWhiteSpace(helper))
            stack.Add(Body(helper, 14, true));
        stack.Add(error);
        return stack;
    }

    Button CreateButton(string text, bool primary, Action action, string? icon = null)
    {
        var button = ButtonStyle(text, primary);
        if (icon != null)
        {
            button.ImageSource = Icon(icon, primary);
            button.ContentLayout = new Button.ButtonContentLayout(
                Button.ButtonContentLayout.ImagePosition.Left, 8);
        }
        BindButtonAction(button, action);
        return button;
    }

    Button CreateAsyncButton(string text, bool primary, Func<Task> action, string? icon = null)
    {
        var button = ButtonStyle(text, primary);
        if (icon != null)
        {
            button.ImageSource = Icon(icon, primary);
            button.ContentLayout = new Button.ButtonContentLayout(
                Button.ButtonContentLayout.ImagePosition.Left, 8);
        }
        BindButtonAction(button, async () =>
        {
            button.IsEnabled = false;
            try
            {
                await action();
            }
            finally
            {
                button.IsEnabled = true;
            }
        });
        return button;
    }

    void BindButtonAction(Button button, Action action)
    {
        button.Pressed += (_, _) => button.Scale = 0.98;
        button.Released += (_, _) => button.Scale = 1;
        button.Clicked += (_, _) => action();
    }

    Entry Field(
        string placeholder,
        Action<string> changed,
        Keyboard? keyboard = null,
        Func<char, bool>? allowCharacter = null
    )
    {
        var e = new Entry
        {
            Placeholder = placeholder,
            Keyboard = keyboard ?? Keyboard.Default,
            FontFamily = "Inter",
            FontSize = 16,
            TextColor = ThemeColor("#202823", "#F4F7FB"),
            PlaceholderColor = ThemeColor("#566257", "#BFCADA"),
            BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
            MinimumHeightRequest = 52,
        };
        e.TextChanged += (_, args) =>
        {
            string value = args.NewTextValue ?? "";
            if (allowCharacter != null && !value.All(allowCharacter))
            {
                e.Text = string.Concat(value.Where(allowCharacter));
                return;
            }

            changed(value);
        };
        return e;
    }

    Editor FieldEditor(string placeholder, Action<string> changed)
    {
        var e = new Editor
        {
            Placeholder = placeholder,
            FontFamily = "Inter",
            FontSize = 16,
            TextColor = ThemeColor("#202823", "#F4F7FB"),
            PlaceholderColor = ThemeColor("#566257", "#BFCADA"),
            BackgroundColor = ThemeColor("#FFFFFF", "#202833"),
            HeightRequest = 112,
        };
        e.TextChanged += (_, a) => changed(a.NewTextValue ?? "");
        return e;
    }
}
