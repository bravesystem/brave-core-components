namespace BRaVe_Management_Backend.DTOs
{
    public class DistributionKitCreateDto
    {
        public int TenantId { get; set; }

        public int DistributionTypeId { get; set; }

        public long KitTypeId { get; set; }

        public int Quantity { get; set; }
        public int UoM { get; set; }
    }
}
