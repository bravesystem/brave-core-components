
using System.Text.Json;


namespace BRaVe_Portal.Models.DTOs
{


    public class IndicatorPreviewRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
        public bool Required { get; set; }
        public int? LookupId { get; set; } // For select options
    }
}
