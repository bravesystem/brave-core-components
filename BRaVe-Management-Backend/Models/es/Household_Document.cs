using BRaVe_Management_Backend.Helpers;
using DocumentFormat.OpenXml.Office2013.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text.Json.Serialization;

namespace BRaVe_Management_Backend.Models.es
{
    public class Household_Document
    {

        [JsonPropertyName("activity_id")]
        public string? ActivityId { get; set; } //ok

        [JsonPropertyName("tenant_id")]
        public int TenantId { get; set; } //ok

        [JsonPropertyName("household_id")]
        public string HouseholdId { get; set; } //ok

        [JsonPropertyName("household_location")]
        public string? HouseholdLocation { get; set; } //ok

        [JsonPropertyName("address")]
        public string? Address { get; set; } //ok

        [JsonPropertyName("household_size")]
        public int HouseholdSize { get; set; } //ok

        [JsonPropertyName("residence_status")]
        public string? ResidenceStatus { get; set; } //ok

        [JsonPropertyName("recipient_type")]
        public string? RecipientType { get; set; }

        [JsonPropertyName("gps_coordinates")]
        public Coordinates? GpsCoordinates { get; set; }

        [JsonPropertyName("datapoints")]
        public List<Datapoint_Document> DataPoints { get; set; } = new();

        [JsonPropertyName("registered_on")]
        public DateTime? RegisteredOn { get; set; } //ok

        [JsonPropertyName("updated_on")]
        public DateTime? UpdatedOn { get; set; } //ok

        [JsonPropertyName("members")]
        public List<Member_Document> Members { get; set; } = new();

        [JsonPropertyName("surveys")]
        public List<Survey_Document> Surveys { get; set; } = new();

        [JsonIgnore]
        public Dictionary<string, object?>? ComputedValues { get; set; }


    }

    public class Coordinates
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("accuracy")]
        public double? Accuracy { get; set; } = -1;
    }
    public class Member_Document
    {
        [JsonPropertyName("uuid")]
        public Guid UUID { get; set; }

        [JsonPropertyName("member_no")]
        public int MemberNo { get; set; }

        [JsonPropertyName("member_id")]
        public string MemberId { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("full_name")]
        public string FullName =>
                string.Join(" ", new[] { FirstName, MiddleName, LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

        [JsonPropertyName("middle_name")]
        public string? MiddleName { get; set; }

        [JsonPropertyName("household_role")]
        public string HouseholdRole { get; set; }

        [JsonPropertyName("gender")]
        public string Gender { get; set; }

        // ISO8601 in your sample -> safe to parse
        [JsonPropertyName("date_of_birth")]
        public DateTime? DateOfBirth { get; set; }

        [JsonPropertyName("age_in_years")]
        public int? AgeInYears { get; set; }

        [JsonPropertyName("age_in_months")]
        public int? AgeInMonths { get; set; }

        [JsonPropertyName("age_in_days")]
        public int? AgeInDays { get; set; }

        [JsonPropertyName("current_age")]
        public AgeDto CurrentAge
        {
            get
            {
                var today = DateTime.UtcNow.Date;

                if (DateOfBirth.HasValue)
                {
                    return DomainHelper.CalculateAgeFromDob(DateOfBirth.Value, today);
                }

                if (AgeInYears.HasValue && RegisteredOn.HasValue)
                {
                    return DomainHelper.CalculateApproximateAge(
                        AgeInYears ?? 0,
                        AgeInMonths ?? 0,
                        AgeInDays ?? 0,
                        RegisteredOn.Value,
                        today
                    );
                }

                return new AgeDto
                {
                    Years = AgeInYears ?? 0,
                    Months = AgeInMonths ?? 0,
                    Days = AgeInDays ?? 0
                };
            }
        }

        [JsonPropertyName("photo_collected")]
        public bool PhotoCollected { get; set; }

        // Base64 or null
        [JsonPropertyName("photo")]
        public string? Photo { get; set; }

        [JsonPropertyName("biometric_collected")]
        public bool BiometricCollected { get; set; }

        // Base64 or null
        [JsonPropertyName("fingerprints")]
        public string? Fingerprints { get; set; }

        [JsonPropertyName("datapoints")]
        public List<Datapoint_Document> DataPoints { get; set; } = new();

        [JsonPropertyName("is_dedup_checked")]
        public bool IsDedupChecked { get; set; } = false;

        [JsonPropertyName("is_duplicate")]
        public bool IsDuplicate { get; set; }

        [JsonPropertyName("matching_id")]
        public string? MatchingId { get; set; }

        // Non-ISO placeholders in sample -> keep as string
        [JsonPropertyName("matched_on")]
        public DateTime? MatchedOn { get; set; }

        [JsonPropertyName("registered_on")]
        public DateTime? RegisteredOn { get; set; }

        [JsonPropertyName("updated_on")]
        public DateTime? UpdatedOn { get; set; }

        [JsonPropertyName("surveys")]
        public List<Survey_Document> Surveys { get; set; } = new();

        [JsonPropertyName("distributions")]
        public List<Distribution_Document> Distributions { get; set; } = new();

        [JsonIgnore]
        public Dictionary<string, object?>? ComputedValues { get; set; }

    }

    public class AgeDto
    {
        [JsonPropertyName("years")]
        public int Years { get; init; }

        [JsonPropertyName("months")]
        public int Months { get; init; }

        [JsonPropertyName("days")]
        public int Days { get; init; }
    }

    public class Datapoint_Document
    {
        [JsonPropertyName("id")]
        public int DatapointId { get; set; }

        [JsonPropertyName("question_text")]
        public string QuestionText { get; set; }

        [JsonPropertyName("answer_type")]
        public string Type { get; set; }

        [JsonPropertyName("question_answer")]
        public string? Answer { get; set; }
    }

    public class Survey_Document
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("answers")]
        public List<Question_Answer_Document> Answers { get; set; }
    }

    public class Question_Answer_Document
    {
        [JsonPropertyName("id")]
        public int QuestionId { get; set; }

        [JsonPropertyName("question_text")]
        public string QuestionText { get; set; }

        [JsonPropertyName("answer_type")]
        public string Type { get; set; }

        [JsonPropertyName("question_answer")]
        public string? Answer { get; set; }
    }

    public class Distribution_Document
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("is_enrolled")]
        public bool IsEnrolled { get; set; }

        [JsonPropertyName("enrolled_on")]
        public DateTime? EnrolledOn { get; set; }

        [JsonPropertyName("has_received")]
        public bool HasReceived { get; set; }

        [JsonPropertyName("received_on")]
        public DateTime? ReceivedOn { get; set; }

        [JsonPropertyName("received_at")]
        public Coordinates? ReceivedAt { get; set; }

        [JsonPropertyName("items_received")]
        public string? ItemsReceived { get; set; }

    }

    /*public static class SearhIndexHelper
    {
        public static SearchIndexOptions getIndexOptions()
        {
            return new SearchIndexOptions
            {

                IndexName = "households_v1",
                Fields =
                [
                    new SearchField { Name = "tenant_id", Type = SearchFieldType.Number },
                    new SearchField { Name = "household_id", Type = SearchFieldType.Keyword },
                    new SearchField { Name = "household_location", Type = SearchFieldType.Text },
                    new SearchField { Name = "address", Type = SearchFieldType.Text },
                    new SearchField { Name = "household_size", Type = SearchFieldType.Number },
                    new SearchField { Name = "residence_status", Type = SearchFieldType.Keyword },
                    new SearchField { Name = "recipient_type", Type = SearchFieldType.Keyword },
                    new SearchField { Name = "registered_on", Type = SearchFieldType.Date },
                    new SearchField { Name = "updated_on", Type = SearchFieldType.Date },

                    new SearchField { Name = "member_first_names", Type = SearchFieldType.Text },
                    new SearchField { Name = "member_last_names", Type = SearchFieldType.Text },
                    new SearchField { Name = "member_genders", Type = SearchFieldType.Keyword },
                    new SearchField { Name = "member_ages_years", Type = SearchFieldType.Number },

                    new SearchField { Name = "datapoint_questions", Type = SearchFieldType.Text },
                    new SearchField { Name = "datapoint_answers", Type = SearchFieldType.Text },

                    new SearchField { Name = "survey_titles", Type = SearchFieldType.Text },
                    new SearchField { Name = "distribution_titles", Type = SearchFieldType.Text }
                ]

            };
        }
    }
*/
}
