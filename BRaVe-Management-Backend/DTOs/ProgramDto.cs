namespace BRaVe_Management_Backend.DTOs
{
    public class ProgramDto
    {
        public int? ProgramId { get; set; }
        public string? ProgramCode { get; set; }
        public int? AssessmentId { get; set; }
        public int TenantId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedOn { get; set; }
        public int StatusId { get; set; }
        public List<UserProgAssignmentDto>? UserProgAssignments { get; set; }

    }
}
