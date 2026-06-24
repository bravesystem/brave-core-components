namespace BRaVe_Mobile_Backend.Models
{
    public class Distribution
    {
        public int DistributionId { get; set; }
        public string ExternalId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<Kit> kits { get; set; }
    }
}