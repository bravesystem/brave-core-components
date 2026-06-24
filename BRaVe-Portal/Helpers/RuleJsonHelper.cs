using System.Text.Json;
using BRaVe_Portal.Models.DTOs;

namespace BRaVe_Portal.Helpers
{
    public static class RuleJsonHelper
    {
        public static RuleGroupDto DeserializeToRootGroup(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new RuleItemConverter() }
            };

            json = json.Trim();

            // JSON array (conditions or groups)
            if (json.StartsWith("["))
            {
                var items = JsonSerializer.Deserialize<List<object>>(json, options)
                            ?? new List<object>();

                return new RuleGroupDto
                {
                    Combinator = "AND",
                    Rules = items
                };
            }

            // Already a group
            if (json.StartsWith("{"))
            {
                return JsonSerializer.Deserialize<RuleGroupDto>(json, options)
                       ?? new RuleGroupDto();
            }

            return new RuleGroupDto();
        }
        public static void NormalizeRules(RuleGroupDto group)
        {
            if (group == null) return;

            // Move Conditions into Rules
            var conditionsProp = group.GetType().GetProperty("Conditions");
            if (conditionsProp != null)
            {
                var conditions = conditionsProp.GetValue(group) as IEnumerable<object>;
                if (conditions != null)
                {
                    group.Rules.AddRange(conditions);
                    conditionsProp.SetValue(group, null);
                }
            }

            // Recursively normalize child groups using a copy of the list
            var rulesCopy = group.Rules.ToList(); // <- copy to avoid modifying during enumeration
            for (int i = 0; i < rulesCopy.Count; i++)
            {
                var item = rulesCopy[i];

                if (item is JsonElement je && je.ValueKind == JsonValueKind.Object && je.TryGetProperty("Combinator", out _))
                {
                    // Deserialize inner group
                    var childGroup = je.Deserialize<RuleGroupDto>();
                    // Replace in the original list
                    int index = group.Rules.IndexOf(item);
                    group.Rules[index] = childGroup!;
                    NormalizeRules(childGroup!);
                }
                else if (item is RuleGroupDto child)
                {
                    NormalizeRules(child);
                }
            }
        }


    }

}
