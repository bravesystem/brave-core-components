
using System.Text.Json;


namespace BRaVe_Portal.Models.DTOs
{


    public class TargetingPreviewResponseDto
    {
        public Guid RunId { get; set; }
        public int Caseload { get; set; }
        public List<TargetingPreviewResultDto> Results { get; set; } = new();

    }
}