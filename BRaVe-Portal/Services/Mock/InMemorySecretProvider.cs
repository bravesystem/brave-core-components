using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;

namespace BRaVe_Portal.Services.Mock
{
    public class InMemorySecretProvider : ISecretProvider
    {
        private readonly Dictionary<string, string> _secrets = new(StringComparer.OrdinalIgnoreCase);

        public InMemorySecretProvider(IConfiguration config)
        {
            string connectionString = config.GetConnectionString("Redis");

            _secrets[KeyVaultSecretNames.Storage.RedisConnection] = connectionString;
        }

        public Task<byte[]?> GetCertificateBytesAsync(string name, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<string?> GetSecretAsync(string name, CancellationToken ct = default)
       => Task.FromResult(_secrets.TryGetValue(name, out var v) ? v : null);
    }
}
