using System.Text.Json.Serialization;

namespace BRaVe_Mobile_Backend.Models
{
    public class DeletionLog
    {
        [JsonPropertyName("id")]
        public long id { get; set; } 

        [JsonPropertyName("activityCode")]
        public string activityCode { get; set; } 

        [JsonPropertyName("deletionType")]
        public string deletionType { get; set; }

        [JsonPropertyName("householdId")]
        public string householdId { get; set; }  

        [JsonPropertyName("individualId")]
        public int individualId { get; set; }

        [JsonPropertyName("deletedIndividualIds")]
        public string deletedIndividualIds { get; set; } 

        [JsonPropertyName("justification")]
        public string justification { get; set; } 

        [JsonPropertyName("deletedBy")]
        public string deletedBy { get; set; } 

        [JsonPropertyName("deletedAt")]
        public long deletedAt { get; set; } 
    }
}
