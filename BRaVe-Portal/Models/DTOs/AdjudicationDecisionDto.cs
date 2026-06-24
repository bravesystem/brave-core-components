namespace BRaVe_Portal.Models.DTOs
{
    public class AdjudicationDecisionDto
    {
        public int DecisionId { get; set; }
        public string LanguageCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
