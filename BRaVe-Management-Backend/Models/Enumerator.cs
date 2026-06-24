using System.ComponentModel.DataAnnotations;

namespace BRaVe_Management_Backend.Models
{
    public class Enumerator
    {
        public int EnumeratorId { get; set; }

        public string EnumeratorCode { get; set; }

        public string? FullName { get; set; }

        public string? Note { get; set; }

        public int TenantId { get; set; }

        public string EnumeratorType { get; set; } = "E";

        public bool IsActive { get; set; } = true;

        public bool IsPinUpdated { get; set; } = false;
        //public bool IsSupervisor { get; set; } = false;
        public bool IsSupervisor
        {
            get => string.Equals(EnumeratorType, "S", StringComparison.OrdinalIgnoreCase);
            set => EnumeratorType = value ? "S" : "E";
        }
        public string CreatedByUserId { get; set; }
        
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        
        public string? UpdatedByUserId { get; set; }

        public DateTime? UpdatedOn { get; set; }

    }
}
