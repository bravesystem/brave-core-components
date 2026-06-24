namespace BRaVe_Management_Backend.DTOs
{
    public class HouseholdMemberDto
    {
        public int IndividualId { get; set; }

        public string? FullName { get; set; }

        public int? AgeYears { get; set; }

        public string? Gender { get; set; }

        public string? Relationship { get; set; }

        public string? Photo { get; set; }

        public string? Fingerprint { get; set; }

        public int IndividualSurveys { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}
