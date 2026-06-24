using System.Text.Json.Serialization;

namespace BRaVe_Mobile_Backend.Models.data_payload
{
    public class Individual
    {
        [JsonPropertyName("uuid")]
        public Guid Uuid { get; set; }    
        // UNIQUEIDENTIFIER PRIMARY KEY
        [JsonPropertyName("householdId")]
        public string HouseholdId { get; set; }  
        // UNIQUEIDENTIFIER NOT NULL
        [JsonPropertyName("individualId")]
        public int IndividualId { get; set; }             // INT NOT NULL

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; }             // NVARCHAR(255) NULL

        [JsonPropertyName("middleName")]
        public string MiddleName { get; set; }            // NVARCHAR(255) NULL

        [JsonPropertyName("lastName")]
        public string LastName { get; set; }              // NVARCHAR(255) NULL

        [JsonPropertyName("dob")]
        public object Dob { get; set; }                // DATE NULL

        [JsonPropertyName("ageInYears")]
        public object AgeInYears { get; set; }              // INT NULL

        [JsonPropertyName("ageInMonths")]
        public object AgeInMonths { get; set; }             // INT NULL

        [JsonPropertyName("ageInDays")]
        public object AgeInDays { get; set; }               // INT NULL

        [JsonPropertyName("relationship")]
        public int Relationship { get; set; }            // INT NULL

        [JsonPropertyName("gender")]
        public int Gender { get; set; }                  // INT NULL
                                                         //public string DataPoints { get; set; }            // NVARCHAR(MAX)
        [JsonPropertyName("dpAnswers")]
        public Dictionary<int, string> dataPoints { get; set; }                 // NVARCHAR(MAX)

        public string ExternalIndividualId { get; set; }  // NVARCHAR(100) NULL

        [JsonPropertyName("photoBase64")]
        public string PhotoBase64 { get; set; }           // NVARCHAR(MAX) NULL

        [JsonPropertyName("biometricBase64")]
        public string BiometricBase64 { get; set; }           // NVARCHAR(MAX) NULL

        [JsonPropertyName("biometricNotCollected")]
        public BiometricNotCollected biometricNotCollected { get; set; }

        public int tenantId { get; set; }                 // INT NOT NULL

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
