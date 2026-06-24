namespace BRaVe_Management_Backend.DTOs
{
    public class LookupTableNameDto
    {
        public int? Id { get; set; }
        public int? TenantId { get; set; }
        public string LookupName { get; set; } = string.Empty;
        public bool? IsCustom { get; set; }
    }
}
