using System.Text.Json.Serialization;

namespace BRaVe_Mobile_Backend.DTOs
{
    public class TokenRefreshRequest
    {
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; }

        [JsonPropertyName("enumerator")]
        public string Enumerator { get; set; }

        [JsonPropertyName("tokenRefresh")]
        public string TokenRefresh { get; set; }

        [JsonPropertyName("nonce")]
        public string Nonce { get; set; }

        [JsonPropertyName("dPoP")]
        public string DPoP { get; set; }
    }
}
