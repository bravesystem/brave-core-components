using System.Text.Json.Serialization;

namespace BRaVe_Management_Backend.Models.data_payload
{
    public class DistributionSelectionDto
    {

        [JsonPropertyName("familyUuid")]
        public string FamilyUuid { get; set; }

        [JsonPropertyName("distribution")]
        public int Distribution { get; set; }

        [JsonPropertyName("selections")]
        public List<SelectionItem> Selections { get; set; }

        // Maps {"1": true, "2": false, ...}
        [JsonPropertyName("itemSelections")]
        public Dictionary<string, bool> ItemSelections { get; set; }

        [JsonPropertyName("assistanceConfirmed")]
        public bool AssistanceConfirmed { get; set; }

        [JsonPropertyName("biometricConfirmation")]
        public bool BiometricConfirmation { get; set; }

        [JsonPropertyName("photoConfirmation")]
        public bool PhotoConfirmation { get; set; }

        [JsonPropertyName("photoBase64")]
        public string PhotoBase64 { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; }

        [JsonPropertyName("enumerator")]
        public string CreatedBy { get; set; }

    }


    public sealed class SelectionItem
    {
        [JsonPropertyName("uuid")]
        public string Uuid { get; set; }

        [JsonPropertyName("selected")]
        public bool Selected { get; set; }
    }
}
