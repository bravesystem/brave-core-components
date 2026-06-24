namespace BRaVe_Management_Backend.DTOs
{
    public class MissionPreAssignmentDto
    {
        public string UserDetails { get; set; } = string.Empty; // email
        public int TenantId { get; set; }
        public int RoleId { get; set; }
        public string? CreatedBy { get; set; }
    }
}
