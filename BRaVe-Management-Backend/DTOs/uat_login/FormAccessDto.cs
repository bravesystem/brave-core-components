namespace BRaVe_Management_Backend.DTOs
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

    public class UatFormResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public int TenantId { get; set; }
        public string Token { get; set; } = string.Empty;
    }

    public class UatFormResetPasswordAdminDto
    {
        public string Email { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public int TenantId { get; set; }
    }

    public class UatFormEmailOnlyDto
    {
        public string Email { get; set; } = string.Empty;
        public int TenantId { get; set; }
    }
}
