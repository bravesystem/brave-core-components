using System.Collections;
using System.Reflection;

namespace BRaVe_Management_Backend.Helpers
{
    public static class ReflectionHelper
    {
        public static object? GetValue(object source, string propertyPath)
        {
            if (source == null || string.IsNullOrWhiteSpace(propertyPath))
                return null;

            var current = source;
            var segments = propertyPath.Split('.');

            foreach (var segment in segments)
            {
                if (current == null) return null;

                var type = current.GetType();
                var prop = type.GetProperty(
                    segment,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (prop == null)
                    return null;

                current = prop.GetValue(current);
            }

            return current;
        }

        /// <summary>
        /// Determines whether a value is a multi-value field (excluding string).
        /// </summary>
        public static bool IsEnumerable(object value)
        {
            return value is IEnumerable && value is not string;
        }
    }
}
