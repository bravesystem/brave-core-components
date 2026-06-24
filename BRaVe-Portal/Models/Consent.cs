using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models
{
    public class Consent
    {
        public int Id { get; set; }
        public int ProgramId { get; set; }
        [MaxLength(250)]
        public string Title { get; set; }
        
        public string Description { get; set; }
        public bool IsActive { get; set; }
         public DateTime? LastUpdated  { get; set; }

    }
}
