namespace BRaVe_Mobile_Backend.Models.data_payload
{
    public class FlaggedData
    {
        public List<Household> households { get; set; }
        public List<Individual> individuals { get; set; }
        public List<SurveyAnswers> surveys { get; set; }
    }

}
