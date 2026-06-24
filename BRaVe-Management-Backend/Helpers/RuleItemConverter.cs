using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BRaVe_Management_Backend.Helpers
{
    public class RuleItemConverter : JsonConverter<IRuleItem>
    {
        public override IRuleItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            // Leaf node (condition)
            if (root.TryGetProperty("Field", out _))
            {
                return JsonSerializer.Deserialize<RuleConditionDto>(root.GetRawText(), options);
            }

            // Nested group node
            if (root.TryGetProperty("Combinator", out _))
            {
                return JsonSerializer.Deserialize<RuleGroupDto>(root.GetRawText(), options);
            }

            throw new JsonException("Unknown rule item type in JSON.");
        }

        public override void Write(Utf8JsonWriter writer, IRuleItem value, JsonSerializerOptions options)
        {
            if (value is RuleConditionDto condition)
            {
                JsonSerializer.Serialize(writer, condition, options);
            }
            else if (value is RuleGroupDto group)
            {
                JsonSerializer.Serialize(writer, group, options);
            }
        }
    }
}
