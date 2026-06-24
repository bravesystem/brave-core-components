namespace BRaVe_Management_Backend.DTOs
{
    public class TargetingFieldsDto
    {
        public string FieldCode { get; set; }
        public string DisplayName { get; set; }
        public int DataType { get; set; }
        public int TenantId { get; set; }
        public string? TableName { get; set; }
        public int? LookUpId { get; set; }
        public List<TargetingFieldValueDto> Values { get; set; } = new();
    }

}
