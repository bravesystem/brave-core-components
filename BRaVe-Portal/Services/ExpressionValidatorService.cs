using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.Enums;
using System.Text.RegularExpressions;

namespace BRaVe_Portal.Services
{
    public class ExpressionValidatorService : IQuestionValidator
    {
        private static readonly Regex ReferencePattern = new(@"\$\{([a-zA-Z_][a-zA-Z0-9_]*|\d+)\}");

        private readonly Dictionary<string, QuestionType> _coreFields;

        public ExpressionValidatorService(Dictionary<string, QuestionType>? coreFields = null)
        {
            // Allow injection of a dictionary, fallback to empty
            _coreFields = coreFields ?? new Dictionary<string, QuestionType>(StringComparer.OrdinalIgnoreCase);
        }

        public ValidationResult ValidateQuestion(Question question, IReadOnlyList<Question> ordered_list)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(question.Expression))
                return result;

            var matches = ReferencePattern.Matches(question.Expression);

            foreach (Match match in matches)
            {
                string token = match.Groups[1].Value;

                // Case 1: Numeric (question reference)
                if (int.TryParse(token, out int refId))
                {
                    var referenced = ordered_list.FirstOrDefault(q => q.Id == refId);
                    if (referenced == null)
                    {
                        result.Errors.Add(
                            $"Q{question.Id} references Question {refId}, which does not exist or appears later."
                        );
                    }
                }
                // Case 2: Named (core entity field)
                else
                {
                    if (!_coreFields.ContainsKey(token))
                    {
                        result.Errors.Add(
                            $"Q{question.Id} references undefined core field '{token}'."
                        );
                    }
                }
            }

            return result;
        }

        public ValidationResult ValidateSurvey(Survey survey)
        {
            var result = new ValidationResult();

            foreach (var q in survey.Questions.OrderBy(q => q.QuestionOrder))
            {
                var prev = survey.Questions.Where(x => x.QuestionOrder < q.QuestionOrder).ToList();
                var qResult = ValidateQuestion(q, prev);
                result.Errors.AddRange(qResult.Errors);
                break;
            }

            return result;
        }
    }
}
