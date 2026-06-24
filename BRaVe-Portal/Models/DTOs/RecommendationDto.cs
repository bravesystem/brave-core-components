namespace BRaVe_Portal.Models.DTOs
{
    public class RecommendationDto
    {
        public int AssessmentId { get; set; }
        public int AssessmentStatusId { get; set; }
        public bool IsLeg { get; set; }
        public string Note { get; set; }
        public bool IsSubmitted { get; set; } = false;
    }
}
