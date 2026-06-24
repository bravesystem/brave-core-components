using System.Reflection;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.Enums;

namespace BRaVe_Portal.Helpers
{
    public static class CoreFieldHelper
    {
        public static Dictionary<string, QuestionType> BuildCoreFields<T>()
        {
            var dict = new Dictionary<string, QuestionType>(StringComparer.OrdinalIgnoreCase);

            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var qType = MapToQuestionType(prop.PropertyType);
                dict[prop.Name] = qType;
            }

            return dict;
        }

        private static QuestionType MapToQuestionType(Type type)
        {
            if (type == typeof(int) || type == typeof(long))
                return QuestionType.INT;
            if (type == typeof(float))
                return QuestionType.FLOAT;
            if (type == typeof(double) || type == typeof(decimal))
                return QuestionType.DOUBLE;
            if (type == typeof(string))
                return QuestionType.TEXT;
            if (type == typeof(bool))
                return QuestionType.BOOLEAN;

            // fallback if no mapping exists
            return QuestionType.TEXT;
        }
    }
}
