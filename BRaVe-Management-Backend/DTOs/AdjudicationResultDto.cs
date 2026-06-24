namespace BRaVe_Management_Backend.DTOs
{
    public class AdjudicationResultDto
    {
        public long AdjudicationId { get; set; }
        public string SourceStatus { get; set; } = "";
        public string MatchedStatus { get; set; } = "";
    }
}
