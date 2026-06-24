namespace BRaVe_Management_Backend.DTOs
{
    public class AdjudicationRequestDto
    {
        public int TenantId { get; set; }
        public long? JobId { get; set; }

        public Guid SourceUuid { get; set; }  
        public Guid MatchedUuid { get; set; }

        public int DecisionId { get; set; }

        public string? Suggestion { get; set; }
        public string Notes { get; set; } = string.Empty;

        public string AdjudicatedBy { get; set; } = string.Empty;

        public string DedupMode { get; set; }
    }
}
