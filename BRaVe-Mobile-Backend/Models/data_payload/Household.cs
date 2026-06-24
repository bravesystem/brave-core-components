using System.Text.Json.Serialization;

namespace BRaVe_Mobile_Backend.Models.data_payload
{
    public class Household
    {
        [JsonPropertyName("uuid")]
        public Guid uuid { get; set; }                     // UNIQUEIDENTIFIER

        [JsonPropertyName("householdId")]
        public string householdId { get; set; }               // VARCHAR(50)

        [JsonPropertyName("householdNo")]
        public int householdNo { get; set; }                   // INT NOT NULL

        [JsonPropertyName("individualNo")]
        public int individualNo { get; set; }                 // INT NOT NULL

        //public string activityCode;           // VARCHAR(10) NOT NULL

        [JsonPropertyName("registrationToken")]
        public string registrationToken { get; set; }          // NVARCHAR(255)

        [JsonPropertyName("householdSize")]
        public int householdSize { get; set; }                // INT NULL

        [JsonPropertyName("householdType")]
        public int householdType { get; set; }                // INT NULL

        [JsonPropertyName("address")]
        public object locationJson{ get; set; }               // NVARCHAR(250)

        [JsonPropertyName("fromServer")]
        public bool fromServer { get; set; } = false;

        [JsonPropertyName("isReadOnly")]
        public bool isReadOnly { get; set; } = false;

        public string collectionSource = "I";       // VARCHAR(1) NOT NULL (I/E)

        public string externalFamilyId { get; set; }           // NVARCHAR(100)

        [JsonPropertyName("dpAnswers")]
        public Dictionary<int, string> dataPoints { get; set; }                 // NVARCHAR(MAX)

        [JsonPropertyName("gps_coordinates")]
        public object gpsLatLonAccuracy { get; set; }          // NVARCHAR(150)

        public int tenantId { get; set; }                     // INT NOT NULL

        [JsonPropertyName("createdBy")]
        public string createdByUserId { get; set; }            // VARCHAR(50) NOT NULL

        [JsonPropertyName("createdOnMs")]
        public long createdOnMs { get; set; }                // DATETIME NOT NULL

        [JsonPropertyName("updatedBy")]
        public string updatedByUserId { get; set; }            // VARCHAR(50)

        [JsonPropertyName("updatedOnMs")]
        public long updatedOnMs { get; set; }               // DATETIME NULL

    }
}