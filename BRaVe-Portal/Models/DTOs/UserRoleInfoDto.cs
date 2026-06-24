using BRaVe_Portal.Models.Enums;

namespace BRaVe_Portal.Models.DTOs
{
    public class UserRoleInfoDto
    {
        public string? ProfileName { get; set; }
        public string? UserId { get; set; }
        public string? MissionName { get; set; }
        public List<string>? Roles { get; set; }
    }

}
