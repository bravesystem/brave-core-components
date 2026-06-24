using BRaVe_Management_Backend.Helpers;

namespace BRaVe_Management_Backend.DTOs
{
    public sealed class ClaimRequest
    {
        public string SessionCode { get; set; } = "";
        public string DeviceKeyThumbprint { get; set; } = ""; // base64url(32) or hex(64)
        public AttestationKeyDto Attestation { get; set; } = new();

        public static bool IsValid(ClaimRequest r)
        => r != null
            && !string.IsNullOrWhiteSpace(r.SessionCode)
            && Thumbprint.TryParse(r.DeviceKeyThumbprint, out _)
            && r.Attestation != null
            && r.Attestation.ChainPem != null
            && r.Attestation.ChainPem.Count > 0
            && !string.IsNullOrWhiteSpace(r.Attestation.Challenge);
    }

    

    

}
