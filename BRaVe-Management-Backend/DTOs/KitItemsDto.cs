namespace BRaVe_Management_Backend.DTOs
{
    public class KitItemsDto
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int KitID { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string? QRCode { get; set; }
        public string? SKU { get; set; }
        public decimal? Measure { get; set; }
        public string? UoM { get; set; }
        public string? Source { get; set; }
        public string? Notes { get; set; }
        public string? UoMExternalCode { get; set; }
        public bool IsActive { get; set; }
        public string? CreatedByUserId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? UpdatedByUserId { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? ItemTag { get; set; }
    }
}
