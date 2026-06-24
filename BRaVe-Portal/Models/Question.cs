using BRaVe_Portal.Models.Enums;

namespace BRaVe_Portal.Models
{
    public class Question
    {
        public int Id { get; set; }
        public int QuestionOrder { get; set; }
        public QuestionType Type { get; set; } // e.g., "INT", "TEXT"

        public string Text { get; set; }
        public string Expression { get; set; }
    }
}
