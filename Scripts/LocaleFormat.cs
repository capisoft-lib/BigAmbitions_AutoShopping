using System.Globalization;

namespace AutoShopping
{
    internal static class LocaleFormat
    {
        internal static string Money(float amount) =>
            "$" + amount.ToString("N0", CultureInfo.InvariantCulture);

        internal static string Integer(int value) =>
            value.ToString(CultureInfo.InvariantCulture);
    }
}
