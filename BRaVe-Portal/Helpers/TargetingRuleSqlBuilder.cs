
using System.Text;
using System.Text.Json;

public class TargetingRuleSqlBuilder
{
    public string Build(JsonElement ruleJson)
    {
        var sb = new StringBuilder();
        sb.AppendLine(" SELECT DISTINCT FirstName,LastName,ageInYears," +
            "Relationship,gender FROM dbo.tbl_Individuals i INNER JOIN dbo.tbl_Households h" +
            "ON i.householdId = h.uuid");
        //sb.AppendLine("FROM dbo.BRaVe"); 
        sb.AppendLine("WHERE");

        sb.Append(BuildGroup(ruleJson));

        return sb.ToString();
    }

    private string BuildGroup(JsonElement group)
    {
        var combinator = group.GetProperty("combinator").GetString()?.ToUpper() ?? "AND";
        var rules = group.GetProperty("rules");

        var parts = new List<string>();

        foreach (var rule in rules.EnumerateArray())
        {
            if (rule.TryGetProperty("rules", out _))
            {
                // Nested group
                parts.Add("(" + BuildGroup(rule) + ")");
            }
            else
            {
                parts.Add(BuildCondition(rule));
            }
        }

        return string.Join($" {combinator} ", parts);
    }

    private string BuildCondition(JsonElement rule)
    {
        var field = rule.GetProperty("field").GetString();
        var op = rule.GetProperty("operator").GetString();
        var valueElement = rule.GetProperty("value");

        return op switch
        {
            "equals" => $"{field} = {FormatValue(valueElement)}",
            "not_equals" => $"{field} <> {FormatValue(valueElement)}",
            "like" => $"{field} LIKE '%{valueElement.GetString()}%'",
            "not_like" => $"{field} NOT LIKE '%{valueElement.GetString()}%'",
            "greater_than" => $"{field} > {valueElement}",
            "greater_than_or_Equal_to" => $"{field} >= {valueElement}",
            "less_than" => $"{field} < {valueElement}",
            "less_than_or_eqaul_to" => $"{field} <= {valueElement}",

            "contains" or "contains_one" or "include_all"
                => $"{field} IN ({FormatArray(valueElement)})",

            "exclude" or "exclude_all"
                => $"{field} NOT IN ({FormatArray(valueElement)})",

            _ => $"-- Unsupported operator: {op}"
        };
    }

    private string FormatValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => $"'{value.GetString()}'",
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.Null => "NULL",
            _ => $"'{value}'"
        };
    }

    private string FormatArray(JsonElement array)
    {
        if (array.ValueKind != JsonValueKind.Array)
            return FormatValue(array);

        var values = array.EnumerateArray()
            .Select(v => FormatValue(v));

        return string.Join(", ", values);
    }
}
