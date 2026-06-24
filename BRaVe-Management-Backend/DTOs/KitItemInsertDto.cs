namespace BRaVe_Management_Backend.DTOs
{
    public class KitItemInsertDto
    {
        public int KitId { get; set; }
        public long ItemId { get; set; }
        public decimal Measure { get; set; }
        public int UoM { get; set; }
        public int TenantId { get; set; }
    }
}
