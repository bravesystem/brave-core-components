
using System.Text.Json;


namespace BRaVe_Portal.Models.DTOs
{

    public class IndicatorValueDto
    {
        public int IndicatorId { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; } // Raw / Composite / System
        public decimal? Value { get; set; }
    }

}