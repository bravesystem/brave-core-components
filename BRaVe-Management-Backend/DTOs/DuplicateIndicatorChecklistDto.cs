namespace BRaVe_Management_Backend.DTOs
{
    public class DuplicateIndicatorChecklistDto
    {

        public int Id { get; set; }
        public int IndicatorId { get; set; }
        public int ExclusiveGroupId { get; set; }
        public decimal Score { get; set; }
        public int? TenantId { get; set; }
        public string Name { get; set; }
    }
}
