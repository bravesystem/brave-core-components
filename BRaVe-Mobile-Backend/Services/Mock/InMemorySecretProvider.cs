using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;

namespace BRaVe_Mobile_Backend.Services.Mock
{
    public class InMemorySecretProvider : ISecretProvider
    {
        private readonly Dictionary<string, string> _secrets = new(StringComparer.OrdinalIgnoreCase);


        public InMemorySecretProvider(IConfiguration config)
        {
            string connectionString = config.GetConnectionString("DefaultConnection");


            var jwtSection = config.GetSection("Jwt");
            var issuer = jwtSection["Issuer"];
            var enrollAudience = jwtSection["Enroll_Audience"];
            var refreshAudience = jwtSection["Refresh_Audience"];
            var apiBaseUrl = jwtSection["ApiBaseUrl"];
            //var key = jwtSection["Key"];



            _secrets[KeyVaultSecretNames.Sql.PrimaryConnection] = connectionString;
            _secrets[KeyVaultSecretNames.Jwt.Issuer] = issuer;
            _secrets[KeyVaultSecretNames.Jwt.Enroll_Audience] = enrollAudience;
            _secrets[KeyVaultSecretNames.Jwt.Refresh_Audience] = refreshAudience;
            _secrets[KeyVaultSecretNames.Jwt.API_BASE_URL] = apiBaseUrl;
            //_secrets[KeyVaultSecretNames.Jwt.Key] = key;


            var vault = config.GetSection("Vault");
            var vaultUrl = vault["Url"];
            var pubKey = vault["Pub_Key"];

            _secrets[KeyVaultSecretNames.SecureStore.Key_Vault] = vaultUrl;
            _secrets[KeyVaultSecretNames.SecureStore.Pub_KeyName] = pubKey;

        }

        public string GetSecretAsync(string name, CancellationToken ct = default)
        => _secrets.TryGetValue(name, out var v) ? v : null;
    }
}
