namespace BRaVe_Portal.Interfaces
{
    public interface ISecretProvider
    {
        Task<string?> GetSecretAsync(string name, CancellationToken ct = default);
        Task<byte[]?> GetCertificateBytesAsync(string name, CancellationToken ct = default);

    }
}
