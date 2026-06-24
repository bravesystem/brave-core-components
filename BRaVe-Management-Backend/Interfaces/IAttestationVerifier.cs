using BRaVe_Management_Backend.DTOs;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IAttestationVerifier
    {
        Task<bool> VerifyHardwareAsync(AttestationKeyDto dto, byte[] expectedThumbSha256);
    }
}
