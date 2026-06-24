namespace BRaVe_Management_Backend.DTOs
{
    public class AdjudicationHistoryDto
    {
        //---------------------------------------
        // HISTORY
        //---------------------------------------

        public long HistoryId { get; set; }

        public long AdjudicationId { get; set; }

        public Guid SourceUuid { get; set; }

        public Guid? MatchedUuid { get; set; }

        public string InitialState { get; set; } = default!;

        public string InitialStateName { get; set; } = default!;

        public string FinalState { get; set; } = default!;

        public string FinalStateName { get; set; } = default!;



        //---------------------------------------
        // SOURCE
        //---------------------------------------

        public string SourceFullName { get; set; } = default!;

        public int? SourceAge { get; set; }

        public string SourceGender { get; set; } = default!;

        public string? SourceRelationship { get; set; }

        public string? SourceHouseholdId { get; set; }

        public int? SourceHouseholdSize { get; set; }

        public string? SourcePhoto { get; set; }

        public string? SourceMission { get; set; }

        public string? SourceGps { get; set; }

        public DateTime? SourceRegistrationDate { get; set; }

        public string? SourceStatus { get; set; }



        //---------------------------------------
        // MATCHED
        //---------------------------------------

        public string? MatchedFullName { get; set; }

        public int? MatchedAge { get; set; }

        public string? MatchedGender { get; set; }

        public string? MatchedRelationship { get; set; }

        public string? MatchedHouseholdId { get; set; }

        public int? MatchedHouseholdSize { get; set; }

        public string? MatchedPhoto { get; set; }

        public string? MatchedMission { get; set; }

        public string? MatchedGps { get; set; }

        public DateTime? MatchedRegistrationDate { get; set; }

        public string? MatchedStatus { get; set; }



        //---------------------------------------
        // MATCH INFO
        //---------------------------------------

        public int? MatchScore { get; set; }

        public decimal? MatchProbability { get; set; }

        public string MatchType { get; set; } = default!;



        //---------------------------------------
        // ADJUDICATION
        //---------------------------------------

        public int DecisionId { get; set; }

        public string DecisionName { get; set; } = default!;

        public string? DecisionDescription { get; set; }

        public string DedupMode { get; set; } = default!;

        public string DedupModeName { get; set; } = default!;

        public string? AdjudicatedBy { get; set; }

        public DateTime AdjudicatedOn { get; set; }

        public string? Suggestion { get; set; }

        public string? Notes { get; set; }



        //---------------------------------------
        // BIOMETRIC
        //---------------------------------------

        public long? BiometricMatchId { get; set; }

        public int? BiometricMatchStatus { get; set; }

        public int? BiometricDecisionId { get; set; }

        public DateTime? BiometricMatchedOn { get; set; }



        //---------------------------------------
        // MANUAL
        //---------------------------------------

        public long? ManualResultId { get; set; }
    }
}