using System.Text.Json.Serialization;

namespace BRaVe_Mobile_Backend.DTOs
{
    public class IntegrityTokenRequest
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }
    }
}
