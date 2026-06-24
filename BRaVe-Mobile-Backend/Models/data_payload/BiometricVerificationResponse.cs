using System.Text.Json.Serialization;

namespace BRaVe_Mobile_Backend.Models.data_payload
{
    public class BiometricVerificationResponse
    {
        [JsonPropertyName("uuid")]
        public string uuid { get; set; }
        [JsonPropertyName("is_processed")]
        public bool is_processed { get; set; }
        [JsonPropertyName("match_found")]
        public bool match_found { get; set; }
        [JsonPropertyName("matched_uuid")]
        public string matched_uuid { get; set; }
        [JsonPropertyName("score")]
        public int score { get; set; }
        [JsonPropertyName("is_enrolled")]
        public int is_enrolled { get; set; } = 0;
        [JsonPropertyName("matched")]
        public FamilyProfile matched { get; set; }
    }
}
