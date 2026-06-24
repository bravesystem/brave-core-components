using System.Text.Json.Serialization;

namespace BRaVe_Mobile_Backend.DTOs
{
    public class TokenRefreshResponse
    {
        [JsonPropertyName("jwt")]
        public string Jwt { get; set; } = "";

        [JsonPropertyName("refresh")]
        public string Refresh { get; set; } = "";
    }
}
