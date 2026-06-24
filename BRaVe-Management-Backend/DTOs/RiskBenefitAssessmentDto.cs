namespace BRaVe_Management_Backend.DTOs
{
    public class RiskBenefitAssessmentDto
    {
        public int TenantId { get; set; }
        public int ProgramId { get; set; }
        public bool RequireApprovals { get; set; } = true;
        public string ProgamManagerName { get; set; }
        public string ProgamManagerContacts { get; set; }
    }
}
