namespace BRaVe_Mobile_Backend.Models
{
    public class Translation
    {
        public int? QuestionId { get; set; }
        public string? SurveyCode { get; set; }
        public string Language { get; set; }
        public string Text { get; set; }
    }
}