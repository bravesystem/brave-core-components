namespace BRaVe_Portal.Models.DTOs
{
    public class AdjudicationRequestDto
    {
        public long? JobId { get; set; }

        public Guid SourceUuid { get; set; }
        public Guid MatchedUuid { get; set; }

        public int DecisionId { get; set; }

        public string? Suggestion { get; set; }

        public string Notes { get; set; } = "";

        public string DedupMode { get; set; } = "A";

    }
}
