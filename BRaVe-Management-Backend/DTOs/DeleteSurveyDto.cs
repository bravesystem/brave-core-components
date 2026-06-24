namespace BRaVe_Management_Backend.DTOs
{
    public class DeleteSurveyDto
    {
        public int SurveyId { get; set; }
        public int TenantId { get; set; } = 0;
    }
}
