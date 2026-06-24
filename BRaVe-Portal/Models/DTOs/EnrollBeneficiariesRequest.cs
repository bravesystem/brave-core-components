namespace BRaVe_Portal.Models.DTOs
{
    public class EnrollBeneficiariesDto
    {
        public int? TenantId { get; set; }
        public int DistributionId { get; set; }
        public int? TargetingId { get; set; }
        public List<string> HouseholdIds { get; set; } = new();
        public string? CreatedBy { get; set; }
    }


}
