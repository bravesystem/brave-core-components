namespace BRaVe_Management_Backend.Models
{
    public class RiskBenefitAssessment
    {
        public int AssessmentId { get; set; }
        //public int TenantId { get; set; }
        public string PmName { get; set; }
        public bool RequireApprovals { get; set; } = true;
        public int StatusId { get; set; }
        public string StatusText { get; set; }

        public string CreatedByUserId { get; set; }

        public DateTime CreatedOn { get; set; }

    }
}
