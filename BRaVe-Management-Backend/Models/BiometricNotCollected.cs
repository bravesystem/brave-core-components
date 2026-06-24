using System.Text.Json.Serialization;

namespace BRaVe_Management_Backend.Models
{
    public class BiometricNotCollected
    {
        [JsonPropertyName("selectedReason")]
        public int selectedReason { get; set; }

        [JsonPropertyName("reasonIfOther")]
        public string reasonIfOther { get; set; }
    }
}
