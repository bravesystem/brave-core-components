namespace BRaVe_Portal.Models.ViewModels
{
    public class UserNote
    {
        public int AssessmentId { get; set; }
        public int TenantId { get; set; }
        public string Note { get; set; }
        public string Decision { get; set; }
        public string CreatedByUserId { get; set; }
        public int CreatedByRole { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}