using System.Text.Json.Serialization;

namespace BRaVe_Mobile_Backend.Models.data_payload
{

    public class ConsentFeedback
    {
        [JsonPropertyName("consent_id")]
        public Guid ConsentId { get; set; }

        [JsonPropertyName("data")]
        public string Data { get; set; } = "";

        // JSON has "inserted_by": "EN0001" (a string), so this must be string:
        [JsonPropertyName("inserted_by")]
        public string InsertedBy { get; set; } = "";

        [JsonPropertyName("inserted_on")]
        public long InsertedOn { get; set; }
    }

    public sealed class ConsentFeedbackDataBundle
    {
        [JsonPropertyName("answers")]
        public Dictionary<int, bool> answers { get; set; } = new Dictionary<int, bool>();

        [JsonPropertyName("comment")]
        public string comment { get; set; }

        [JsonPropertyName("type")]
        public int type { get; set; } = 0;

        [JsonPropertyName("consentNotProvided")]
        public bool consentNotProvided { get; set; } = true;

        public ConsentFeedbackEntry getData()
        {
            return new ConsentFeedbackEntry
            {
                Answers = answers,
                Comment = comment,
            };
        }

    }

    public class ConsentFeedbackEntry
    {
        public Dictionary<int, bool> Answers { get; set; } = new Dictionary<int, bool>();

        public string Comment { get; set; }

    }

}
