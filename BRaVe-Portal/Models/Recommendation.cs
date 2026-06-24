namespace BRaVe_Portal.Models
{
    public class Recommendation
    {
        public int AssessmentId { get; set; }
        public bool IsLeg { get; set; }
        public string Note { get; set; }
        public bool IsSubmitted { get; set; } = false;
    }
}
