using BRaVe_Portal.Models;

namespace BRaVe_Portal.Interfaces
{
    public interface IQuestionValidator
    {
        ValidationResult ValidateQuestion(Question question, IReadOnlyList<Question> ordered_list);

        ValidationResult ValidateSurvey(Survey survey);
    }
}
