using System.Text.Json.Serialization;

namespace BRaVe_Management_Backend.Models.data_payload
{
    public class BiometricVerificationRequest
    {
        [JsonPropertyName("uuid")]
        public string uuid { get; set; }

        [JsonPropertyName("gender")]
        public int gender { get; set; }

        [JsonPropertyName("template")]
        public string template { get; set; }

        [JsonPropertyName("createdBy")]
        public string createdByUserId { get; set; }

        [JsonPropertyName("createdOnMs")]
        public long createdOnMs { get; set; }

    }

}
