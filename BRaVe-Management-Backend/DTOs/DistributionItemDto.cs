namespace BRaVe_Management_Backend.DTOs
{
    public class DistributionItemDto
    {
        public int? Id { get; set; }
        public int TenantId { get; set; }

        public string SKU { get; set; }
        public string Name { get; set; }
        public string? Source { get; set; }
        public bool IsActive { get; set; }
        public DateTime? UpdatedOn { get; set; }

    }
}
