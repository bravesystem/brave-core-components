
using System.Text.Json;


namespace BRaVe_Portal.Models.DTOs
{

    public class IndicatorPreviewDto
    {
        public Guid EntityId { get; set; }
        public List<IndicatorValueDto> Indicators { get; set; } = new();
        public decimal TotalScore { get; set; }
        public bool Eligible { get; set; }
        public int Rank { get; set; }
        public List<IndicatorBadgeDto> Badges { get; set; } = new();

    }
}