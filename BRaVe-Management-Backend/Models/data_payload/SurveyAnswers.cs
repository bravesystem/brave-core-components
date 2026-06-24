using System.Text.Json.Serialization;

namespace BRaVe_Management_Backend.Models.data_payload
{
    public class SurveyAnswers
    {
        [JsonPropertyName("survey_id")]
        public int survey_id { get; set; }

        [JsonPropertyName("household_id")]
        public string household_id { get; set; }

        [JsonPropertyName("individual_id")]
        public int individual_id { get; set; }

        [JsonPropertyName("data")]
        public string answers { get; set; }
    }
}
