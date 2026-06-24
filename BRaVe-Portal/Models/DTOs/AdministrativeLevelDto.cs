using BRaVe_Portal.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models.DTOs
{
    public class AdministrativeLevelDto
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        [MaxLength(100)]
        public string LevelName { get; set; }
        [MaxLength(50)]
        public string? OfficialCode { get; set; }=null;

        public bool IsActive { get; set; } = true;
        public DateTime? UpdatedOn { get; set; }


    }
}
