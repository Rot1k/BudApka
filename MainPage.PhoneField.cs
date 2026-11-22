namespace BudApka;

public partial class MainPage
{
    View PhoneField(string value, Action<string> changed, bool required)
    {
        string displayed = InputRules.FormatPhoneTail(value);
        return LabeledField(
            "Номер телефону",
            displayed,
            "66 487 72 62",
            "Введіть 9 цифр після +380 або вставте повний номер.",
            _ => { },
            Keyboard.Numeric,
            validate: text => !required && text.Length == 0
                ? null : InputRules.ValidatePhone(InputRules.UkrainianPhonePrefix + text),
            configureEntry: ConfigurePhoneEntry,
            validateOnUnfocus: false,
            prefix: InputRules.UkrainianPhonePrefix);

        void UpdatePhone(string digits) =>
            changed(digits.Length == 0 ? "" : InputRules.UkrainianPhonePrefix + digits);

        void ConfigurePhoneEntry(Entry entry)
        {
#if WINDOWS
            entry.HandlerChanged += (_, _) =>
            {
                if (entry.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.TextBox textBox)
                {
                    textBox.BeforeTextChanging -= RejectInvalidPhoneEdit;
                    textBox.BeforeTextChanging += RejectInvalidPhoneEdit;
                }
            };
#endif
            entry.TextChanged += (_, args) =>
            {
                string input = args.NewTextValue ?? "";
                if (input == displayed)
                {
                    return;
                }

                if (InputRules.IsPhoneDeletion(displayed, input))
                {
                    displayed = input;
                    UpdatePhone(string.Concat(input.Where(InputRules.Digit)));
                    return;
                }

                int editStart = 0;
                while (editStart < Math.Min(input.Length, displayed.Length)
                    && input[editStart] == displayed[editStart])
                {
                    editStart++;
                }

                int editEnd = input.Length;
                int previousEnd = displayed.Length;
                while (editEnd > editStart && previousEnd > editStart
                    && input[editEnd - 1] == displayed[previousEnd - 1])
                {
                    editEnd--;
                    previousEnd--;
                }

                if (!InputRules.TryReadPhoneTail(input, out string digits))
                {
                    entry.Text = displayed;
                    entry.CursorPosition = previousEnd;
                    return;
                }

                int digitsAfterEdit = input[editEnd..].Count(InputRules.Digit);
                int digitsBeforeCursor = Math.Max(0, digits.Length - digitsAfterEdit);
                displayed = InputRules.FormatPhoneTail(digits);
                if (input != displayed)
                {
                    entry.Text = displayed;
                    entry.CursorPosition = PhoneCursorPosition(displayed, digitsBeforeCursor);
                }

                UpdatePhone(digits);
            };
        }
    }

#if WINDOWS
    private static void RejectInvalidPhoneEdit(
        Microsoft.UI.Xaml.Controls.TextBox sender,
        Microsoft.UI.Xaml.Controls.TextBoxBeforeTextChangingEventArgs args)
    {
        args.Cancel = !InputRules.IsPhoneDeletion(sender.Text, args.NewText)
            && !InputRules.TryReadPhoneTail(args.NewText, out _);
    }
#endif

    private static int PhoneCursorPosition(string displayed, int digitCount)
    {
        int index = 0;
        while (index < displayed.Length && digitCount > 0)
        {
            if (InputRules.Digit(displayed[index]))
            {
                digitCount--;
            }

            index++;
        }

        return index;
    }
}
