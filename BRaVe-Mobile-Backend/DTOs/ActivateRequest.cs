namespace BRaVe_Mobile_Backend.DTOs
{
    public sealed class ActivateRequest
    {
        public string Username { get; set; } = "";
        public int TenantId { get; set; }
        public string EnrollmentJws { get; set; } = "";
        public string DevicePublicKeyPem { get; set; } = ""; 

        public static bool IsValid(ActivateRequest r)
            => r is not null
            && !string.IsNullOrWhiteSpace(r.Username)
            && r.TenantId > 0
            && !string.IsNullOrWhiteSpace(r.EnrollmentJws)
            && !string.IsNullOrWhiteSpace(r.DevicePublicKeyPem);
    }
}
