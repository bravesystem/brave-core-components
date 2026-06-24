namespace BRaVe_Management_Backend.DTOs
{
    public class TempAccessTableDto
    {
        public string UserId { get; set; } = default!;
        public int MissionId { get; set; }
        public int RoleId { get; set; }
        public DateTimeOffset? RequestedAt { get; set; }
        public DateTimeOffset? AccessedAt { get; set; }
        public DateTimeOffset? LogoutTime { get; set; }
        public string? Reason { get; set; }
        public bool IsConsumed { get; set; }
    }
}
