namespace BRaVe_Management_Backend.Models
{
    public class ROHQDecision
    {
        public int ApprovalId { get; set; }
        public int AssessmentId { get; set; }
        public int TenantId { get; set; }
        public bool IsSubmitted { get; set; }
        public string? Note { get; set; }
    }
}
