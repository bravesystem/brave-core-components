namespace BRaVe_Management_Backend.Models
{
    public class DuplicateMatchResult
    {
        public Guid MemberUuidA { get; set; }
        public string MemberIdA { get; set; } = string.Empty;
        public string DocumentIdA { get; set; } = string.Empty;
        public string FullnameA { get; set; } = string.Empty;
        public int? AgeA { get; set; }
        public string GenderA { get; set; } = string.Empty;
        public bool PictureCollectedA { get; set; }
        public bool BiometricCollectedA { get; set; }
        public string HouseholdIdA { get; set; } = string.Empty;
        public DateTime? RegisteredOnA { get; set; }

        public Guid MemberUuidB { get; set; }
        public string MemberIdB { get; set; } = string.Empty;
        public string DocumentIdB { get; set; } = string.Empty;
        public string FullnameB { get; set; } = string.Empty;
        public int? AgeB { get; set; }
        public string GenderB { get; set; } = string.Empty;
        public bool PictureCollectedB { get; set; }
        public bool BiometricCollectedB { get; set; }
        public string HouseholdIdB { get; set; } = string.Empty;
        public DateTime? RegisteredOnB { get; set; }

        public double TotalScore { get; set; }
    }

    public class DuplicateMatchResultsViewModel
    {
        public double MinScore { get; set; } = 0;
        public double MaxScore { get; set; } = 0;
        public List<DuplicateMatchResult> Records { get; set; } = new();

    }
}
