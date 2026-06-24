using BRaVe_Portal.Models.Enums;

namespace BRaVe_Portal.Models.DTOs
{
    public class RiskBenefitAssessmentDto
    {
        public int AssessmentId { get; set; }
        public int ProgramId { get; set; }
        public int TenantId { get; set; }
        public bool RequireApprovals { get; set; } 
        public string CreatedByUserId { get; set; }
        public DateTime CreatedOn { get; set; }
        public int StatusId { get; set; } 
    }
}
