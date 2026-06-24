namespace BRaVe_Management_Backend.DTOs
{
    public class StatusChangeDto
    {
        public int AssessmentId { get; set; }
        public int FromStatus { get; set; }
        public int ToStatus { get; set; }
    }
}
