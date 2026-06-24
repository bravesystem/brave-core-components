namespace BRaVe_Mobile_Backend.Interfaces
{
    public interface INonceService
    {
        Task saveNonce(Guid nonceId, string deviceId,byte[] hashedNonce, string? purpose, TimeSpan ttl, string? metadata);
        Task<bool> validateNonce(Guid nonceId, byte[] hashNonce, string? UsedByIp);
    }
}
