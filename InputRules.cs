using System.Text.RegularExpressions;

namespace BudApka;

internal static class InputRules
{
    public const int PhoneTailLength = 9;
    public const string UkrainianPhonePrefix = "+380";

    public static bool Digit(char value) => char.IsAsciiDigit(value);

    public static string GetPhoneTail(string value)
    {
        if (value.StartsWith(UkrainianPhonePrefix, StringComparison.Ordinal))
        {
            value = value[UkrainianPhonePrefix.Length..];
        }
        else if (value.StartsWith('0'))
        {
            value = value[1..];
        }

        return string.Concat(value.Where(Digit).Take(PhoneTailLength));
    }

    // A null replacement tells the native field to apply its edit unchanged.
    public static string? FilterPhoneEdit(string current, string insertion, int start, int end)
    {
        if (insertion.Length == 0)
        {
            return null;
        }

        bool replacesAll = start == 0 && end == current.Length;
        if (replacesAll && TryNormalizePhone(insertion, out string normalized))
        {
            return normalized[UkrainianPhonePrefix.Length..];
        }

        int available = PhoneTailLength - (current.Length - (end - start));
        if (insertion.All(Digit) && insertion.Length <= available
            && (start != 0 || insertion[0] != '0'))
        {
            return null;
        }

        if (replacesAll && insertion.All(c => Digit(c) || IsPhoneSeparator(c)))
        {
            string digits = string.Concat(insertion.Where(Digit));
            if (digits.Length == PhoneTailLength && digits[0] != '0')
            {
                return digits;
            }
        }

        // Preserve a selection when an invalid replacement is rejected.
        return current[start..end];
    }

    public static bool NameCharacter(char value) =>
        char.IsLetter(value) || " '’ʼ-‐‑".Contains(value)
        || char.GetUnicodeCategory(value) == System.Globalization.UnicodeCategory.NonSpacingMark;

    public static bool AddressCharacter(char value) =>
        NameCharacter(value) || char.IsAsciiDigit(value) || ".,/#№·–()".Contains(value);

    public static string? ValidatePhone(string value) =>
        TryNormalizePhone(value, out _)
            ? null : "Введіть український номер у форматі 0671234567 або +380671234567.";

    public static bool TryNormalizePhone(string value, out string normalized)
    {
        normalized = "";
        if (!value.All(c => Digit(c) || c == '+' || IsPhoneSeparator(c)))
        {
            return false;
        }

        string compact = string.Concat(value.Where(c => !IsPhoneSeparator(c)));
        if (!Regex.IsMatch(compact, @"\A(?:\+38)?0[1-9][0-9]{8}\z"))
        {
            return false;
        }

        normalized = compact.Length == 10 ? "+38" + compact : compact;
        return true;
    }

    static bool IsPhoneSeparator(char value) =>
        char.IsWhiteSpace(value) || value is '(' or ')' or '-' or '‐' or '‑' or '–';

    public static string? ValidateName(string value) =>
        string.IsNullOrWhiteSpace(value)
        || Regex.IsMatch(value.Trim(), @"\A\p{L}[\p{L}\p{M}]*(?:[ '’ʼ‐‑-]\p{L}[\p{L}\p{M}]*)*\z")
            ? null : "Введіть ім’я літерами. Дозволені пробіл, апостроф і дефіс.";

    public static string? ValidateDistrict(string value) =>
        value.Any(char.IsLetter) && value.All(AddressCharacter)
            ? null : "Вкажіть населений пункт і район, якщо він є.";

    public static string? ValidateAddress(string value)
    {
        string building = Regex.Split(
            value.Trim(), @"\s*,?\s+(?:кв(?:артира)?\.?|офіс|під['’ʼ]їзд)\s*",
            RegexOptions.IgnoreCase)[0];
        // A flat number after a comma cannot stand in for the building number.
        return value.All(AddressCharacter) && building.Any(char.IsLetter)
            && Regex.IsMatch(building, @"\A.*[ ,№][0-9]+[\p{L}]?(?:[-/][0-9]+[\p{L}]?)?\s*\z")
            ? null : "Вкажіть вулицю та номер будинку, наприклад «вул. Лісова 12, кв. 8».";
    }
}
