namespace BRaVe_Management_Backend.DTOs
{
    public class IndicatorBadgeDto
    {
        public int Id { get; set; }
        public int TenantId { get; set; }

        public string Description { get; set; }


        public bool IsActive { get; set; } = true;

        public string? CreatedByUserId { get; set; }

        public DateTime? CreatedOn { get; set; } = DateTime.UtcNow;

        public string? UpdatedByUserId { get; set; }

        public DateTime? UpdatedOn { get; set; }

    }
}
