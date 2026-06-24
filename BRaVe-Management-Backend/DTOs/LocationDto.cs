namespace BRaVe_Management_Backend.DTOs
{
    public class LocationDto
    {
        public int Id { get; set; }
        public int TenantId { get; set; }

        public string LocationName { get; set; }
        public string OfficialCode { get; set; }
        public int LevelId { get; set; }
        public int? ParentLocationId { get; set; }

        public bool IsActive { get; set; } = false;
        public DateTime? UpdatedOn { get; set; }

    }
}
