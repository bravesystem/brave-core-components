namespace BRaVe_Management_Backend.DTOs
{
    public class ComputedIndicatorValueDto
    {
        public int RunId { get; set; }
        public int IndicatorId { get; set; }
        public Guid EntityId { get; set; }
        public decimal? Value { get; set; }
        public DateTime CalculatedOn { get; set; } = DateTime.UtcNow;
    }

}
