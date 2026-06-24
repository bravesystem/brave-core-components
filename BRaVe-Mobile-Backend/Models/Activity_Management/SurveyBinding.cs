namespace BRaVe_Mobile_Backend.Models
{
    public class SurveyBinding
    {
        public int SurveyId { get; set; }
        public bool IsRequired { get; set; }

        public SurveyBinding(int surveyId, bool isRequired)
        {
            SurveyId = surveyId;
            IsRequired = isRequired;
        }
    }
}