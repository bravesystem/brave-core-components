using BRaVe_Portal.Models;

namespace BRaVe_Portal.Helpers
{
    public static class QuestionValidatorHelper
    {
        public static IReadOnlyList<Question> GetAllBefore(Question question, List<Question> questions)
        {
            return questions
                .Where(q => q.QuestionOrder < question.QuestionOrder)
                .OrderBy(q => q.QuestionOrder)
                .ToList();
        }
    }
}
