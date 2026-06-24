using BRaVe_Portal.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models.DTOs
{
    public class KitItemInsertDto
    {
        public int KitId { get; set; }
        public long ItemId { get; set; }
        public decimal Measure { get; set; }
        public int UoM { get; set; }
        public int TenantId { get; set; }
    }
}
