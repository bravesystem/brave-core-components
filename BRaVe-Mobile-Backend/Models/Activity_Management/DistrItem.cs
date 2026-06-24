using System.Text.Json.Serialization;

namespace BRaVe_Mobile_Backend.Models
{
    public class DistrItem
    {  
        public int DistributionId { get; set; }
        public int KitId { get; set; }
        public int Id { get; set; }
        [JsonPropertyName("sku")]
        public string SKU { get; set; }
        public string ExternalId { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }
        public double Quantity { get; set; }
        public string UoM { get; set; }

    }
}
