using BRaVe_Portal.Helpers;
using BRaVe_Portal.Models.Enums;
using StackExchange.Redis;

namespace BRaVe_Portal.Models
{
    public class Survey
    {
        public int SurveyId { get; set; }
        public int ProgramId { get; set; }
        public string ProgramName { get; set; }
        public string SurveyCode { get; set; }
        public int SurveyType { get; set; }
        public int? TenantId { get; set; }
        public string? Title { get; set; }
        public string Details { get; set; }
        public bool? IsActive { get; set; }
        public string CreatedByUserId { get; set; }
        public string UpdatedByUserId { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }

        public int TotalQuestions { get; set; } = 0;

        public List<Question> Questions { get; set; } = new List<Question>();

        public override string ToString()
        {
            return $"{SurveyId}-{Title}";
        }
    }
}
