using System.ComponentModel.DataAnnotations;

namespace BRaVe_Management_Backend.Models
{
    public class Consent
    {
  

        public int Id { get; set; }
        public int ProgramId { get; set; }
        public int TenantId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }

        public DateTime LastUpdated => CreatedOn > (UpdatedOn ?? DateTime.MinValue)
                     ? CreatedOn
                        : UpdatedOn.Value;



    }
}
