
using System.Text.Json;


namespace BRaVe_Portal.Models.DTOs
{


    public class IndicatorPreviewResponseDto
    {
        public int RunId { get; set; }
        public int Caseload { get; set; }
        public List<IndicatorPreviewDto> Results { get; set; } = new();

    }
}