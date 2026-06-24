namespace BRaVe_Mobile_Backend.Models
{
    public class Survey
    {
        public int SurveyId { get; set; }
        public string? SurveyCode { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Type { get; set; }    
        public bool IsActive { get; set; }
        public List<Question> Questions { get; set; } = new();

    }
}