namespace BRaVe_Portal.Models.DTOs
{
    public class DuplicateIndicatorChecklistDto
    {
        public int Id { get; set; }
        public int IndicatorId { get; set; }
        public int ExclusiveGroupId { get; set; }
        public decimal Score { get; set; }
        public int? TenantId { get; set; }
        public string Name { get; set; }
        public bool Selected { get; set; } = false;
        public bool Disabled { get; set; } = true;
    }

    public static class DefaultIndicators
    {
        public static int HighScore = 1;
        public static int NotHighScore = 2;
        public static int MatchingNames = 3;
        public static int NamesNotMatching = 4;
        public static int MatchingGender = 5;
        public static int GenderNotMatching = 6;
        public static int MatchingAge = 7;
        public static int AgeGapLt5 = 8;
        public static int AgeGapGt10 = 9;
        public static int AgeGapElse = 10;
        public static int MathingAllMembers = 11;
        public static int MatchingSomeMembers = 12;
        public static int MembersNotMatching = 13;
        public static int ManualDeduplication = 14;

    }
}



/*
 public int MatchScore { get; set; }
public int Status { get; set; }
public DateTime MatchedOn { get; set; }
public bool SameAge => MatchedAge == SourceAge;
public bool AgeGapOver10 => Math.Abs(MatchedAge.Value - SourceAge.Value) > 10;
public bool AgeGapBelow5 => Math.Abs(MatchedAge.Value - SourceAge.Value)>0 && Math.Abs(MatchedAge.Value - SourceAge.Value) < 5;
public bool AgeGapBetween5and10 => Math.Abs(MatchedAge.Value - SourceAge.Value) >= 5 && Math.Abs(MatchedAge.Value - SourceAge.Value) <= 10;
public bool SameGender => MatchedGender == SourceGender;
public bool HighScore => MatchScore >= 200;

public bool MatchingFamSize => SourceHHSize==MatchedHHSize;
 */