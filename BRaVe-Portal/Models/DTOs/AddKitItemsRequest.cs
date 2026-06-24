using BRaVe_Portal.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models.DTOs
{
    public class AddKitItemsRequest
    {
        public long KitId { get; set; }
        public int TenantId { get; set; }
        public string CreatedByUserId { get; set; }
        public List<KitItemInsertDto> Items { get; set; }
    }
}
