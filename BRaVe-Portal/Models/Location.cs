using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models
{
    public class Location
    {
        public int Id { get; set; }
        public int TenantId { get; set; }

        [MaxLength(200)]
        public string LocationName { get; set; }

        [MaxLength(50)]
        public string? OfficialCode { get; set; }
        public int LevelId { get; set; }
        public int? ParentLocationId { get; set; }
        public string? ParentLocationName { get; set; }

        public bool IsActive { get; set; } = false;
        public DateTime? UpdatedOn { get; set; }
        public DateTime? CreatedOn { get; set; }

    }
}
