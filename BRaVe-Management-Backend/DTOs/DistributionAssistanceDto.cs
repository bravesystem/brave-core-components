namespace BRaVe_Management_Backend.DTOs
{
    public class DistributionAssistanceDto
    {
        public int Id { get; set; }
        public int? TenantId { get; set; }
        public int? KitId { get; set; }
        public int? Quantity { get; set; }
        public string? Notes { get; set; }


        public int? DistributionID { get; set; }


        public bool IsActive { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? KitName { get; set; }
        public string? ItemTag { get; set; }

        public int? ItemTagID { get; set; }
    }
}
