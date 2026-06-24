using BRaVe_Portal.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models.DTOs
{
    public class LocationDto
    {
        public int Id { get; set; }
        public int LevelId { get; set; }
        
               public int TenantId { get; set; }
        public int? ParentLocationId { get; set; }

        [MaxLength(200)]
        public string? LocationName { get; set; }
        [MaxLength(50)]
        public string? OfficialCode { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? UpdatedOn { get; set; }


    }
}
