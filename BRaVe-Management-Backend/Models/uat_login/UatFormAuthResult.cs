namespace BRaVe_Management_Backend.Models
{
    public class UatFormAuthResult
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int TenantId { get; set; }
        public string? Token { get; set; } = string.Empty;  //not needed

    }
}
