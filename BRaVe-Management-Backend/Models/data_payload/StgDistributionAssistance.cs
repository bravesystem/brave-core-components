using System.Text.Json.Serialization;

namespace BRaVe_Management_Backend.Models.data_payload
{
    public class StgDistributionAssistance
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
