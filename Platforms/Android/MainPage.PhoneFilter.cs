using Android.Text;
using Android.Widget;
using Java.Lang;

namespace BudApka;

public partial class MainPage
{
    static void AttachAndroidPhoneFilter(Entry entry)
    {
        entry.HandlerChanged += (_, _) =>
        {
            if (entry.Handler?.PlatformView is EditText editText)
            {
                var filters = editText.GetFilters() ?? [];
                if (!filters.Any(filter => filter is PhoneInputFilter))
                {
                    editText.SetFilters([new PhoneInputFilter(), .. filters]);
                }
            }
        };
    }

    sealed class PhoneInputFilter : Java.Lang.Object, IInputFilter
    {
        public ICharSequence? FilterFormatted(
            ICharSequence? source, int start, int end, ISpanned? destination,
            int destinationStart, int destinationEnd)
        {
            if (start == end)
            {
                return null;
            }

            string incoming = source?.ToString() ?? "";
            string current = destination?.ToString() ?? "";
            if (start < 0 || end < start || end > incoming.Length || destinationStart < 0
                || destinationEnd < destinationStart || destinationEnd > current.Length)
            {
                return new Java.Lang.String("");
            }

            string? replacement = InputRules.FilterPhoneEdit(
                current, incoming[start..end], destinationStart, destinationEnd);
            return replacement == null ? null : new Java.Lang.String(replacement);
        }
    }
}
