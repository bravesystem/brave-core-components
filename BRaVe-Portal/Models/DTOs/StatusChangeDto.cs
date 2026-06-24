namespace BRaVe_Portal.Models.DTOs
{
    public class StatusChangeDto
    {
        public int AssessmentId { get; set; }
        public int FromStatus { get; set; }
        public int ToStatus { get; set; }
    }
}
