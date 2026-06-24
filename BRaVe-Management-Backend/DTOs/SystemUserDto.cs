
    namespace BRaVe_Management_Backend.DTOs
    {
        public class SystemUserDto
        {
            public string UserId { get; set; } = string.Empty;
            public string ProfileName { get; set; } = string.Empty;
            public string MissionName { get; set; }
            public List<string> Roles { get; set; } = new List<string>();

            public List<int> RoleIds { get; set; } = new();
        }
    }


