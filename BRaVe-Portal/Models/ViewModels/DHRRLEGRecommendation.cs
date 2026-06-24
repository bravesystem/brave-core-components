namespace BRaVe_Portal.Models.ViewModels
{
    public class DHRRLEGRecommendation
    {
        public int ApprovalId { get; set; }
        public int AssessmentId { get; set; }
        public int TenantId { get; set; }
        public string Note { get; set; }
        public string CreatedByUserId { get; set; }
        public int CreatedByRole { get; set; }
    }
}