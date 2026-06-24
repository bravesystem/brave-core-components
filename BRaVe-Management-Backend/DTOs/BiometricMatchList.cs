namespace BRaVe_Management_Backend.DTOs
{
    public class BiometricMatchListDto
    {
        public string? SourceActivity { get; set; }
        public Guid SourceUuid { get; set; }
        public string? SourceFullName { get; set; }
        public int? SourceAge { get; set; }
        public string? SourceGender { get; set; }
        public string? SourceRelationship { get; set; }
        public string? SourceHouseholdId { get; set; }
        public DateTime SourceRegistrationDate { get; set; }

        public string? MatchedActivity { get; set; }
        public Guid MatchedUuid { get; set; }
        public string? MatchedFullName { get; set; }
        public int? MatchedAge { get; set; }
        public string? MatchedGender { get; set; }
        public string? MatchedRelationship { get; set; }
        public string? MatchedHouseholdId { get; set; }
        public DateTime MatchedRegistrationDate { get; set; }

        public int MatchScore { get; set; }
        public int Status { get; set; }
        public DateTime MatchedOn { get; set; }
    }
}
