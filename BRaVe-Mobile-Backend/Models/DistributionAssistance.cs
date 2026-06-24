using System.Text.Json.Serialization;

namespace BRaVe_Mobile_Backend.Models
{
    public class DistributionAssistance
    {
        [JsonPropertyName("distributionId")]
        public int distributionId { get; set; }

        [JsonPropertyName("householdId")]
        public string householdId { get; set; }

        [JsonPropertyName("individualId")]
        public int individualId { get; set; }

        [JsonPropertyName("data")]
        public string assistances { get; set; }
    }
}
