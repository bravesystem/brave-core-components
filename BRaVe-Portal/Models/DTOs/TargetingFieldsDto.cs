

using BRaVe_Portal.Models.DTOs;

namespace BRaVe_Management_Backend.DTOs
{
    public class TargetingFieldsDto
    {
        public int IndicatorId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public IndicatorDataType DataType { get; set; }
        public int TenantId { get; set; }
        public string? TableName { get; set; }
        public int? LookUpId { get; set; }

        // If LookUpId != null → dropdown
        // If empty → textbox

         public List<TargetingFieldValueDto> LookupValues { get; set; } = new();
    }

}
