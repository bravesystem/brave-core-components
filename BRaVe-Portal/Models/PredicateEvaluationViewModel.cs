using System.Text.Json.Serialization;

namespace BRaVe_Portal.Models
{
    public class PredicateEvaluationViewModel
    {
        public List<PredicateEvaluation> Evaluations { get; set; } = new();
    }

    public class PredicateEvaluation
    {
        public DuplicateRule rule { get; set; }
        public bool EvaluationResult { get; set; }

        public class DuplicateRule
        {
            public int? Id { get; set; }
            public string? Description { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("fields")]
            public List<string> Fields { get; set; } = new();

            [JsonPropertyName("op")]
            public string Op { get; set; } = "eq"; // eq, ne, fuzzy, diff_lte, diff_lt, diff_gte, contains

            /// <summary>
            /// Optional threshold for operators like diff_lte (max allowed difference), fuzzy (min similarity).
            /// </summary>
            [JsonPropertyName("value")]
            public double? Value { get; set; }

            [JsonPropertyName("score")]
            public double Score { get; set; }
        }
    }
}
