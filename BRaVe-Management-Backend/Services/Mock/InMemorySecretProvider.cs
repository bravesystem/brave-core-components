using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;

namespace BRaVe_Management_Backend.Services.Mock
{
    public class InMemorySecretProvider : ISecretProvider
    {
        private readonly Dictionary<string, string> _secrets = new(StringComparer.OrdinalIgnoreCase);


        public InMemorySecretProvider(IConfiguration config)
        {
            string connectionString = config.GetConnectionString("DefaultConnection");

            _secrets[KeyVaultSecretNames.Sql.PrimaryConnection] = connectionString;

            //var jwtSection = config.GetSection("Jwt");
            //var issuer = jwtSection["Issuer"];
            //var enrollAudience = jwtSection["Enroll_Audience"];
            //var refreshAudience = jwtSection["Refresh_Audience"];
            //var apiBaseUrl = jwtSection["ApiBaseUrl"];
            //var key = jwtSection["Key"];


            var vault = config.GetSection("Vault");
            var vaultUrl = vault["Url"];
            var pubKey = vault["Pub_Key"];

            _secrets[KeyVaultSecretNames.SecureStore.Key_Vault] = vaultUrl;
            _secrets[KeyVaultSecretNames.SecureStore.Pub_KeyName] = pubKey;
        }

        public Task<byte[]?> GetCertificateBytesAsync(string name, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<string?> GetSecretAsync(string name, CancellationToken ct = default)
        => Task.FromResult(_secrets.TryGetValue(name, out var v) ? v : null);
    }
}
