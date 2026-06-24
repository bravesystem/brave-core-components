namespace BRaVe_Management_Backend.DTOs.claim_session
{
    public class CreatePrintSessionResponse
    {
        public long SessionId { get; set; }
        public string SessionCode { get; set; } = "";
        public int TenantId { get; set; }
        public int MaxDevices { get; set; }
        public int DevicesConnected { get; set; }
        public string Status { get; set; } = "";
    }
}
