namespace BRaVe_Management_Backend.DTOs
{
    public class LookupTableValueDto
    {
        public int? Id { get; set; }
        public int? TenantId { get; set; }
        public int LookupId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int? Order { get; set; }

        public bool IsCustom { get; set; } = false;

        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
