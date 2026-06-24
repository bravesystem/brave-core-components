namespace BRaVe_Portal.Models.ViewModels
{
    public class DistributionViewModel
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public int TenantId { get; set; }
        public int? Quantity { get; set; }
        public string Description { get; set; }
        public string? Notes { get; set; }
        public string? Code { get; set; }
        public bool IsActive { get; set; }
        public DateTime? UpdatedOn { get; set; }

    }
}