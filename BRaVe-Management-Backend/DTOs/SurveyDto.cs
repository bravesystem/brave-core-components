namespace BRaVe_Management_Backend.DTOs
{
    public class SurveyDto
    {
        public int? SurveyId { get; set; }
        public int ProgramId { get; set; }
        public int SurveyType { get; set; }  // or enum if defined
        public int TenantId { get; set; } = 0;
        public string Title { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;   // replaces "Description"
        public bool IsActive { get; set; }

    }
}
