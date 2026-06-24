namespace BRaVe_Management_Backend.DTOs
{
    public class CreateTargetingRuleDto
    {
        public int TenantId { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // Full rule tree JSON
        public string RuleJson { get; set; } = string.Empty;

    }
}
