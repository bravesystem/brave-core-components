namespace BRaVe_Management_Backend.Interfaces
{
    public interface ISecretProvider
    {
        Task<string?> GetSecretAsync(string name, CancellationToken ct = default);
        //Task SetSecretAsync(string name, string value, CancellationToken ct = default); // optional (for tests/tools)
        Task<byte[]?> GetCertificateBytesAsync(string name, CancellationToken ct = default); // optional
    }

}
