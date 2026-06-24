using BRaVe_Portal.Models.Enums;

namespace BRaVe_Portal.Models.DTOs
{
    public class DashboardSurveyDetailDto
    {
        public string? HouseholdId { get; set; }
        public int? IndividualId { get; set; }
        public int? AnswerType { get; set; }
        public int QuestionId { get; set; }
        public string Question { get; set; }
        public string QuestionAnswer { get; set; }
        public int SurveyId { get; set; }
        public string SurveyName { get; set; }
    }
}
