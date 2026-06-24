namespace BRaVe_Management_Backend.DTOs.claim_session
{
    public class PrintSessionRecord
    {
        public long SessionId { get; }
        public int TenantId { get; }
        public int MaxDevices { get; }
        public int DevicesConnected { get; }

        public PrintSessionRecord(long sessionId, int tenantId, int maxDevices, int devicesConnected)
        {
            SessionId = sessionId;
            TenantId = tenantId;
            MaxDevices = maxDevices;
            DevicesConnected = devicesConnected;
        }
    }
}
