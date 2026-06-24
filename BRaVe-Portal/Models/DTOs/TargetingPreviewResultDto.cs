
using System.Text.Json;


namespace BRaVe_Portal.Models.DTOs
{

    public class TargetingPreviewResultDto
    {
        public Guid EntityId { get; set; }
        public decimal TotalScore { get; set; }
        public bool Eligible { get; set; }
        public int Rank { get; set; }

        public List<TargetingPreviewIndicatorDto> Indicators { get; set; } = new();
    }

}