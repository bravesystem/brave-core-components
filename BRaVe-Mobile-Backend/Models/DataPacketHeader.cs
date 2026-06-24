using System.Text.Json.Serialization;

namespace BRaVe_Mobile_Backend.Models
{
    public class DataPacketHeader
    {
        //[JsonPropertyName("encryptedKey")]
        public string EncryptedKey { get; set; }

        //[JsonPropertyName("nonce")]
        public string Nonce { get; set; } //Batch Id

        //[JsonPropertyName("activityCode")]
        public string ActivityCode { get; set; }

        //[JsonPropertyName("dPoP")]
        public string DPoP { get; set; }
        public string BatchId { get; set; }

        //[JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        public string? Extra { get; set; }

    }
}
