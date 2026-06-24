namespace BRaVe_Management_Backend.DTOs
{
    public class IndicatorPreviewDto
    {
        public int IndicatorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public IndicatorType Type { get; set; }
        public decimal? Value { get; set; }
        public IndicatorLevel Level { get; set; }
        public List<IndicatorBadgeDto>? Badges { get; set; }
        public bool Eligible { get; set; }
        public decimal TotalScore { get; set; }
    }

}
