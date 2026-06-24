
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using System;

namespace BRaVe_Management_Backend.Services
{
    public class EnvSecretProvider : ISecretProvider
    {
        private readonly Dictionary<string, string> _secrets = new(StringComparer.OrdinalIgnoreCase);

        public EnvSecretProvider(IConfiguration config)
        {
      
            string sql = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Sql.PrimaryConnection) ?? "";

            _secrets[KeyVaultSecretNames.Sql.PrimaryConnection] = sql;

            string redis = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Storage.RedisConnection) ?? "";

            _secrets[KeyVaultSecretNames.Storage.RedisConnection] = redis;

            string vault = Environment.GetEnvironmentVariable(KeyVaultSecretNames.SecureStore.Key_Vault) ?? "";

            _secrets[KeyVaultSecretNames.SecureStore.Key_Vault] = vault;

            string keyname = Environment.GetEnvironmentVariable(KeyVaultSecretNames.SecureStore.Pub_KeyName) ?? "";

            _secrets[KeyVaultSecretNames.SecureStore.Pub_KeyName] = keyname;

        }

        public Task<byte[]?> GetCertificateBytesAsync(string name, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<string?> GetSecretAsync(string name, CancellationToken ct = default)
       => Task.FromResult(_secrets.TryGetValue(name, out var v) ? v : null);
    }
}
