using System.Text.Json.Serialization;

namespace BRaVe_Management_Backend.Models
{
    public class DuplicateCriteriaDefinition
    {
        [JsonPropertyName("rules")]
        public List<DuplicateRule> Rules { get; set; } = new();
    }
}
