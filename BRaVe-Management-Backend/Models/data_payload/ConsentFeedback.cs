using System.Text.Json.Serialization;

namespace BRaVe_Management_Backend.Models.data_payload
{
    public class ConsentFeedback
    {
        [JsonPropertyName("consent_id")]
        public Guid ConsentId { get; set; }

        [JsonPropertyName("data")]
        public string Data { get; set; } = "";

        // JSON has "inserted_by": "EN0001" (a string), so this must be string:
        [JsonPropertyName("inserted_by")]
        public string InsertedBy { get; set; } = "";

        [JsonPropertyName("inserted_on")]
        public long InsertedOn { get; set; }
    }

}
