using System.Text.Json.Serialization;

namespace BRaVe_Mobile_Backend.Models
{
    public class Kit
    {
        public int DistributionId { get; set; }
        public int KitId { get; set; }
        [JsonPropertyName("sku")]
        public string SKU { get; set; }
        public string ExternalId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Source { get; set; }
        public int Quantity { get; set; }
        public string? TargetType { get; set; } = "K";
        public List<DistrItem> items { get; set; }

    }
}
