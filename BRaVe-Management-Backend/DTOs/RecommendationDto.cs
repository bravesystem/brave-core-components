namespace BRaVe_Management_Backend.DTOs
{
    public class RecommendationDto
    {
        public int AssessmentId { get; set; }
        public int AssessmentStatusId { get; set; }
        public int TenantId { get; set; }
        public bool IsLeg { get; set; }
        public string Note { get; set; }
        public bool IsSubmitted { get; set; }
    }
}
