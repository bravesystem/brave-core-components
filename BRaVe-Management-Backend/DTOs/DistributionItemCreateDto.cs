namespace BRaVe_Management_Backend.DTOs
{
    public class DistributionItemCreateDto

    {

        public int TenantId { get; set; }

        public int DistributionTypeId { get; set; }

        public long ItemTypeId { get; set; }

        public int Quantity { get; set; }

        public decimal Measure { get; set; }

        public int UoM { get; set; }


    }
}
