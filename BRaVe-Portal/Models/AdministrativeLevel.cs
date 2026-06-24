using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models
{
    public class AdministrativeLevel
    {
        public int Id { get; set; }
        public int TenantId { get; set; }

        [MaxLength(100)]
        public string? LevelName { get; set; }

        [MaxLength(50)]
        public string? OfficialCode { get; set; }
        public bool IsActive { get; set; } = true;
         public DateTime? UpdatedOn  { get; set; }

    }
}
