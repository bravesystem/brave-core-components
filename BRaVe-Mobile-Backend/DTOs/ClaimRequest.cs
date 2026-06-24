using BRaVe_Mobile_Backend.Helpers;
using System.Text.Json.Serialization;

namespace BRaVe_Mobile_Backend.DTOs
{
    public sealed class ClaimRequest
    {
        [JsonPropertyName("sessionCode")]
        public string SessionCode { get; set; } = "";

        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = "";

        [JsonPropertyName("deviceKeyThumbprint")]
        public string DeviceKeyThumbprint { get; set; } = "";

        public static bool IsValid(ClaimRequest r)
        => r != null
            && !string.IsNullOrWhiteSpace(r.SessionCode)
            && !string.IsNullOrWhiteSpace(r.DeviceId);
            //&& Thumbprint.TryParse(r.DeviceKeyThumbprint, out _);
    }
}
