
using System.Text.Json.Serialization;
namespace BRaVe_Portal.Models.DTOs
{


    public class HouseholdDataDto
    {
        [JsonPropertyName("household_id")]
        public string HouseholdId { get; set; }

        [JsonPropertyName("household_size")]
        public int HouseholdSize { get; set; }

        [JsonPropertyName("household_location")]
        public string? HouseholdLocation { get; set; }

        [JsonPropertyName("members")]
        public List<MemberDto> Members { get; set; } = new();
    }

    public class MemberDto
    {
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("gender")]
        public string Gender { get; set; }

        [JsonPropertyName("current_age")]
        public AgeDto CurrentAge { get; set; }

        [JsonPropertyName("household_role")]
        public string HouseholdRole { get; set; }
    }

    public class AgeDto
    {
        [JsonPropertyName("years")]
        public int Years { get; set; }
    }

}
