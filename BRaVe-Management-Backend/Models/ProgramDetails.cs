namespace BRaVe_Portal.Models
{
    public class ProgramDetails
    {
        public int ProgramId { get; set; }
        public string ProgramCode { get; set; }
        public int AssessmentId { get; set; }
        public int TenantId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string Author { get; set; }
        public DateTime CreatedOn { get; set; } 
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedByUserId { get; set; }
        public int StatusId { get; set; }

        public override string ToString()
        {
            return $"{ProgramId}-{Title}";
        }

    }
}
