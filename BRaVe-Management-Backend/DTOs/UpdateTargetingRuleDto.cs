namespace BRaVe_Management_Backend.DTOs
{
    public class UpdateTargetingRuleDto
    {
        public string RuleName { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

    }
}
