using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;

namespace BRaVe_Mobile_Backend.Services
{
    public class EnvSecretProvider : ISecretProvider
    {

        private readonly Dictionary<string, string> _secrets = new(StringComparer.OrdinalIgnoreCase);

        public EnvSecretProvider()
        {

            string keyvault = Environment.GetEnvironmentVariable(KeyVaultSecretNames.SecureStore.Key_Vault) ?? "";
            _secrets[KeyVaultSecretNames.SecureStore.Key_Vault] = keyvault;


            string pubkey = Environment.GetEnvironmentVariable(KeyVaultSecretNames.SecureStore.Pub_KeyName) ?? "";
            _secrets[KeyVaultSecretNames.SecureStore.Pub_KeyName] = pubkey;


            string sql = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Sql.PrimaryConnection) ?? "";
            _secrets[KeyVaultSecretNames.Sql.PrimaryConnection] = sql;


            string redis = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Storage.RedisConnection) ?? "";
            _secrets[KeyVaultSecretNames.Storage.RedisConnection] = redis;


            string issuer = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Jwt.Issuer) ?? "";
            _secrets[KeyVaultSecretNames.Jwt.Issuer] = issuer;


            var audiences = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Jwt.Enroll_Audience);

            var enrollAudience = audiences.Split(",")[0];
            var refreshAudience = audiences.Split(",")[1];

            _secrets[KeyVaultSecretNames.Jwt.Enroll_Audience] = enrollAudience;
            _secrets[KeyVaultSecretNames.Jwt.Refresh_Audience] = refreshAudience;



        }

        public string GetSecretAsync(string name, CancellationToken ct = default)
            => _secrets.TryGetValue(name, out var v) ? v : null;
    }
}
