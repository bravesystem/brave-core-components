namespace BRaVe_Portal.Models.DTOs
{
    public class UatFormRegisterDto
    {
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string AdminToken { get; set; } = string.Empty;
    }


    public class UatFormLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }


    public class UatFormAuthResult
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int TenantId { get; set; }
        public string? Token { get; set; } = string.Empty;

    }
}
