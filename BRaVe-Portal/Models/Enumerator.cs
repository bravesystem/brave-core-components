using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models

{

    public class Enumerator

    {

        public int EnumeratorId { get; set; }
        [MaxLength(250)]
        public string? FullName { get; set; }
        [MaxLength(550)]
        public string? Note { get; set; }
        [MaxLength(32)]
        public string? EnumeratorPin { get; set; }
        [MaxLength(50)]
        public string? EnumeratorCode { get; set; }

        public string? EnumeratorType { get; set; }

        //public bool IsSupervisor { get; set; } = false;
        //public bool IsSupervisor => string.Equals(EnumeratorType, "S", StringComparison.OrdinalIgnoreCase);
        public bool IsSupervisor
        {
            get => string.Equals(EnumeratorType, "S", StringComparison.OrdinalIgnoreCase);
            set => EnumeratorType = value ? "S" : "E";
        }

        public bool IsActive { get; set; } = true;

        public bool IsPinUpdated { get; set; } = false;

        public DateTime? UpdatedOn { get; set; }

    }

}

