namespace BRaVe_Management_Backend.DTOs.RoleManagement
{
    public class UserRoleInfo
    {
        public string UserId { get; set; }
        public string? Email { get; set; }
        public string ProfileName { get; set; }
        public string MissionName { get; set; }
        public List<string> Roles { get; set; } = new();

        public List<string> RoleIds { get; set; } = new();

    }
}
