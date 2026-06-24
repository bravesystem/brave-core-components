
namespace BRaVe_Portal.Models.DTOs
{
    public class UserProgAssignmentDto
    {
        public string UserId { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public int? TenantId { get; set; }
    }
}


