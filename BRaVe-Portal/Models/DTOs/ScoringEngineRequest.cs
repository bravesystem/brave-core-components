namespace BRaVe_Portal.Models.DTOs
{
    public class ScoringEngineRequest
    {
        public DateRangeDto range { get; set; }

        //public CriteriaDefinition criteria { get; set; }

        public int targetingId { get; set; }
    }
}
