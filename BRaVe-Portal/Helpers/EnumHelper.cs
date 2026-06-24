namespace BRaVe_Portal.Helpers
{
    public static class EnumHelper
    {
        public static string ToString<TEnum>(int value, string defaultValue = "NaN") where TEnum : Enum
        {
            return Enum.IsDefined(typeof(TEnum), value) ? ((TEnum)(object)value).ToString() : defaultValue;
        }
    }
}
