using System.Text.Json.Serialization;

namespace BRaVe_Mobile_Backend.Models
{
    public class BiometricNotCollected
    {
        [JsonPropertyName("selectedReason")]
        public int selectedReason { get; set; }

        [JsonPropertyName("reasonIfOther")]
        public string reasonIfOther { get; set; }

        public BiometricNotCollected() { }

        public BiometricNotCollected(int? selectedReason, string? reasonIfOther) { 

            this.selectedReason = selectedReason.HasValue? selectedReason.Value: 1;

            if (string.IsNullOrEmpty(reasonIfOther)) {
                this.reasonIfOther = "";
            }
            else
            {
                this.reasonIfOther = reasonIfOther;
            }     
        }
    }
}
