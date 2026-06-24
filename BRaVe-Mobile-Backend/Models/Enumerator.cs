using System.Text.Json.Serialization;

namespace BRaVe_Mobile_Backend.Models
{
    public class Enumerator
    {
        public string EnumeratorCode { get; set; }
        public string? FullName { get; set; }
        public string? PhotoBase64 { get; set; }
        public string? Note { get; set; }
        public bool IsActive { get; set; }
        public bool IsPinUpdated { get; set; } 
        public bool IsSupervisor { get; set; }

        [JsonPropertyName("pinB64")]
        public byte[] EnumeratorPin { get; set; }
        public DateTime UpdatedOn { get; set; }

        public long LastUpdated => new DateTimeOffset(UpdatedOn).ToUnixTimeMilliseconds();
    }
}
