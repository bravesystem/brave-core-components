using BRaVe_Portal.Models.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models.DTOs

{

    public class EnumeratorDto
    {
        //public int? EnumeratorId { get; set; }
        [MaxLength(50)]
        public string? EnumeratorCode { get; set; }
        [MaxLength(32)]
        public string? EnumeratorPin { get; set; }
        [MaxLength(250)]
        public string? FullName { get; set; }
        [MaxLength(550)]
        public string? Note { get; set; }

        public string? EnumeratorType { get; set; }

        public int TenantId { get; set; }

        public bool IsSupervisor { get; set; } = false;

        public bool IsPinUpdated { get; set; } = false;

        public bool IsActive { get; set; } = true;

    }

}

