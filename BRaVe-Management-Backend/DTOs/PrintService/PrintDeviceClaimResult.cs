namespace BRaVe_Management_Backend.DTOs.PrintService
{
    public class PrintDeviceClaimResult
    {
        public long SessionId { get; set; }
        public int TenantId { get; set; }

        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
    }
}

