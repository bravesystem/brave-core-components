namespace BRaVe_Portal.Models.DTOs
{
    public class BiometricMatchResultDto
    {
        // Source individual
        public Guid? SourceUuid { get; set; }
        public string SourceFullName { get; set; }
        public int? SourceAge { get; set; }
        public string SourceGender { get; set; }
        public string SourceRelationship { get; set; }
        public string SourceHouseholdId { get; set; }
        public int? SourceHHSize { get; set; }
        public string SourcePhoto { get; set; }
        public string SourceMission { get; set; }
        public string SourceGps { get; set; }
        public DateTime? SourceRegistrationDate { get; set; }

        // Matched individual
        public Guid MatchedUuid { get; set; }
        public string MatchedFullName { get; set; }
        public int? MatchedAge { get; set; }
        public string MatchedGender { get; set; }
        public string MatchedRelationship { get; set; }
        public string MatchedHouseholdId { get; set; }
        public int? MatchedHHSize { get; set; }
        public string MatchedPhoto { get; set; }
        public string MatchedMission { get; set; }
        public string MatchedGps { get; set; }
        public DateTime MatchedRegistrationDate { get; set; }

        // Match info
        public int MatchScore { get; set; }
        public int Status { get; set; }
        public DateTime MatchedOn { get; set; }

        public bool IsManual { get; set; }

        public bool SameAge => MatchedAge == SourceAge;
        public bool AgeGapOver10 => Math.Abs(MatchedAge.Value - SourceAge.Value) > 10;
        public bool AgeGapBelow5 => Math.Abs(MatchedAge.Value - SourceAge.Value)>0 && Math.Abs(MatchedAge.Value - SourceAge.Value) < 5;
        public bool AgeGapBetween5and10 => Math.Abs(MatchedAge.Value - SourceAge.Value) >= 5 && Math.Abs(MatchedAge.Value - SourceAge.Value) <= 10;
        public bool SameGender => MatchedGender == SourceGender;


        public bool HighScore => !IsManual && MatchScore >= 200;
        public bool LowScore => !IsManual && MatchScore < 200;

        public bool MatchingFamSize => SourceHHSize==MatchedHHSize;

    }
}
