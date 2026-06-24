namespace BRaVe_Portal.Models.DTOs
{
    public class DistributionViewDto
    {
        public int DistributionId { get; set; }

        public string HouseholdId { get; set; } = string.Empty;

        // Verification
        public bool SignedWithBiometric { get; set; }

        public bool SignedWithPhoto { get; set; }

        // Only set if SignedWithPhoto == true
        public string? PhotoUrl { get; set; }

        // Items
        public List<DistributedItemDto> Items { get; set; } = new();
    }
}
