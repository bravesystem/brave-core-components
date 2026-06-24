namespace BRaVe_Management_Backend.Models
{
    public class UserNote
    {
        public int AssessmentId { get; set; }
        public string Note { get; set; }
        public string Decision { get; set; }
        public int CreatedByRole { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
