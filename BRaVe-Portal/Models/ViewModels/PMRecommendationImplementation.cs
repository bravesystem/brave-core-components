using BRaVe_Portal.Models.Enums;

namespace BRaVe_Portal.Models.ViewModels
{
    public class PMRecommendationImplementation
    {
        public int LEGApprovalId { get; set; }
        public int AssessmentId { get; set; }
        public int TenantId { get; set; }
        public RecommendationStatus? StatusId { get; set; }
        public string Note { get; set; }
        public string CreatedByUserId { get; set; }
    }

}