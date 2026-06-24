using System.Text.Json;

namespace BRaVe_Management_Backend.Helpers
{
    public static class TargetingRuleSqlBuilder
    {
        public static string Build(JsonElement group)
        {
            var combinator = group.GetProperty("combinator").GetString()?.ToUpper() ?? "AND";
            var rules = group.GetProperty("rules");

            var conditions = new List<string>();

            foreach (var rule in rules.EnumerateArray())
            {
                if (rule.TryGetProperty("rules", out _))
                {
                    conditions.Add($"({Build(rule)})");
                }
                else
                {
                    var field = rule.GetProperty("field").GetString();
                    var op = rule.GetProperty("operator").GetString();
                    var value = rule.GetProperty("value");

                    conditions.Add(BuildCondition(field!, op!, value));
                }
            }

            return string.Join($" {combinator} ", conditions);
        }

        private static string BuildCondition(string field, string op, JsonElement value)
        {
            return op switch
            {
                // ===============================
                // Equality
                // ===============================
                "equals" => $"{field} = {Format(value)}",
                "not_equals" => $"{field} <> {Format(value)}",

                // ===============================
                // Comparison
                // ===============================
                "greater_than" => $"{field} > {Format(value)}",
                "greater_than_or_equal_to" => $"{field} >= {Format(value)}",
                "less_than" => $"{field} < {Format(value)}",
                "less_than_or_equal_to" => $"{field} <= {Format(value)}",

                // ===============================
                // String matching
                // ===============================
                "like" => $"{field} LIKE '%{EscapeLike(value)}%'",
                "not_like" => $"{field} NOT LIKE '%{EscapeLike(value)}%'",

                // ===============================
                // IN / NOT IN
                // ===============================
                "contains_all" => BuildIn(field, value),
                "contains_one" => BuildIn(field, value),
                "contains" => BuildIn(field, value),

                "include_all" => BuildIn(field, value),
                "include_any" => BuildIn(field, value),

                "exclude" => $"{field} NOT IN ({FormatArray(value)})",
                "exclude_all" => $"{field} NOT IN ({FormatArray(value)})",
                "exclude_any" => $"{field} NOT IN ({FormatArray(value)})",

                // ===============================
                // Special cases
                // ===============================
                "phone" => $"{field} LIKE '%{DigitsOnly(value)}%'",

                // ===============================
                // Safe fallback
                // ===============================
                _ => "1 = 1"
            };
        }

        private static string Format(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.True => "1",
                JsonValueKind.False => "0",
                _ => $"'{value.ToString().Replace("'", "''")}'"
            };
        }

        private static string BuildIn(string field, JsonElement array) =>
            $"{field} IN ({FormatArray(array)})";

        private static string FormatArray(JsonElement array)
        {
            if (array.ValueKind != JsonValueKind.Array)
                return Format(array);

            return string.Join(",",
                array.EnumerateArray().Select(v => Format(v))
            );
        }

        private static string EscapeLike(JsonElement value) =>
            value.GetString()
                ?.Replace("'", "''")
                ?.Replace("%", "[%]")
                ?.Replace("_", "[_]") ?? "";

        private static string DigitsOnly(JsonElement value) =>
            new string(value.GetString()?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());
    }
}
