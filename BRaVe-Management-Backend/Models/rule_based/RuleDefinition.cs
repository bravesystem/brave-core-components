using System.Text.Json;
using System.Text.Json.Serialization;

namespace BRaVe_Management_Backend.Models
{
    public class RuleDefinition
    {
        [JsonPropertyName("rules")]
        public List<WeightedRule> Rules { get; set; } = new();
    }

    /// <summary>
    /// A rule with an associated weight (score) and its criteria.
    /// </summary>
    public class WeightedRule
    {
        [JsonPropertyName("weight")]
        public double Weight { get; set; }

        [JsonPropertyName("criteria")]
        public CriteriaNode Criteria { get; set; } = default!;
    }

    /// <summary>
    /// Represents either a composite criteria (AND/OR with sub-rules) or a leaf criteria (field comparison).
    /// </summary>
    public class CriteriaNode
    {
        // Composite criteria: operator + sub-rules
        [JsonPropertyName("operator")]
        public string? Operator { get; set; }

        [JsonPropertyName("rules")]
        public List<CriteriaNode>? Rules { get; set; }

        // Leaf criteria: field comparison
        [JsonPropertyName("field_name")]
        public string? FieldName { get; set; }

        /// <summary>
        /// Type of the field for casting Value from string during operations (e.g. "string", "int", "long", "double", "bool", "datetime").
        /// </summary>
        [JsonPropertyName("field_type")]
        public string? FieldType { get; set; }

        [JsonPropertyName("op")]
        public string? Op { get; set; }

        [JsonPropertyName("value")]
        public JsonElement? Value { get; set; }

        /// <summary>
        /// True if this node is a composite (has operator and sub-rules).
        /// </summary>
        [JsonIgnore]
        public bool IsComposite => !string.IsNullOrEmpty(Operator) && Rules != null;

        /// <summary>
        /// True if this node is a leaf (has field, op, value).
        /// </summary>
        [JsonIgnore]
        public bool IsLeaf => !string.IsNullOrEmpty(FieldName) && !string.IsNullOrEmpty(Op);
    }

}
