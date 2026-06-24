namespace BRaVe_Portal.Models.DTOs
{
    public class DecisionDto
    {
        public int AssessmentId { get; set; }
        public int AssessmentStatusId { get; set; }
        public int? DecisionStatusId { get; set; }
        public string Note { get; set; }
        public bool IsSubmitted { get; set; } = false;
    }
}
