using BRaVe_Management_Backend.DTOs.RoleManagement;

namespace BRaVe_Management_Backend.DTOs
{
    public class TenantAndRolesDto
    {
        public int TenantId { get; set; }

        public IReadOnlyList<int> Roles { get; set; } = new List<int>();

        public UserRoleInfo UserProfile { get; set; }
    }
}
