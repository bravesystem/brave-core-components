namespace BRaVe_Management_Backend.Models
{
    public class Distributions
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public int ProgramId { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }
        public string CreatedByUser { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public string? UpdatedByUser { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
