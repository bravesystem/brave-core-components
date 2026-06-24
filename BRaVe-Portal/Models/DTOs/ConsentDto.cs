using BRaVe_Portal.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models.DTOs
{
    public class ConsentDto
    {        
        public int ProgramId { get; set; }
        [MaxLength(250)] 
        public string Title { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
        public string? DefaultLang { get; set; }
    }
}
