using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BRaVe_Portal.Helpers
{
    public class RuleItemConverter : JsonConverter<object>
    {
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return null;

            // If it has a "field" property -> it's a condition
            if (root.TryGetProperty("field", out _))
            {
                return JsonSerializer.Deserialize<RuleConditionDto>(root.GetRawText(), options);
            }
            else
            {
                // It's a group
                var group = JsonSerializer.Deserialize<RuleGroupDto>(root.GetRawText(), options)!;

                if (root.TryGetProperty("rules", out var rulesElement))
                {
                    group.Rules = new List<object>();
                    foreach (var item in rulesElement.EnumerateArray())
                    {
                        var ruleItem = JsonSerializer.Deserialize<object>(item.GetRawText(), options);
                        if (ruleItem != null)
                            group.Rules.Add(ruleItem);
                    }
                }

                return group;
            }
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
