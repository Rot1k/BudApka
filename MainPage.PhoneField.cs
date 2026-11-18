namespace BudApka;

public partial class MainPage
{
    View PhoneField(string value, Action<string> changed, bool required)
    {
#if !ANDROID
        Entry? phoneEntry = null;
        string accepted = InputRules.GetPhoneTail(value);
#endif
        return LabeledField(
            "Номер телефону",
            InputRules.GetPhoneTail(value),
            "67 123 45 67",
            "Введіть 9 цифр або вставте повний український номер.",
            digits =>
            {
#if !ANDROID
                bool plainEdit = digits.Length <= InputRules.PhoneTailLength
                    && digits.All(InputRules.Digit) && !(accepted.Length == 0 && digits == "0");
                string? replacement = plainEdit ? null : InputRules.FilterPhoneEdit(
                    accepted, digits, 0, accepted.Length);
                if (phoneEntry != null && replacement != null && replacement != digits)
                {
                    phoneEntry.Text = replacement;
                    return;
                }
                accepted = digits;
#endif
                changed(digits.Length == 0 ? "" : InputRules.UkrainianPhonePrefix + digits);
            },
            Keyboard.Numeric,
            validate: digits => !required && digits.Length == 0
                || InputRules.TryNormalizePhone(InputRules.UkrainianPhonePrefix + digits, out _)
                    ? null : "Введіть 9 цифр після +380. Наприклад, 671234567.",
#if ANDROID
            configureEntry: AttachAndroidPhoneFilter,
#else
            configureEntry: entry => phoneEntry = entry,
#endif
            validateOnUnfocus: false,
            prefix: InputRules.UkrainianPhonePrefix);
    }
}
