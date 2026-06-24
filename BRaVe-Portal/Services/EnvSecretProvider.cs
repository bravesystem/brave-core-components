 using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;

namespace BRaVe_Portal.Services
{
    public class EnvSecretProvider : ISecretProvider
    {
        private readonly Dictionary<string, string> _secrets = new(StringComparer.OrdinalIgnoreCase);

        private readonly IAuthModeService _authMode;
        public EnvSecretProvider() 
        {

                var redis = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Storage.RedisConnection) ?? "";

                _secrets[KeyVaultSecretNames.Storage.RedisConnection] = redis;


                var scope = Environment.GetEnvironmentVariable(KeyVaultSecretNames.AzureAd.Fe_Audience_Scope) ?? "";

                _secrets[KeyVaultSecretNames.AzureAd.Fe_Audience_Scope] = scope;
               

                var partnerAccess = Environment.GetEnvironmentVariable(KeyVaultSecretNames.ExternalLinks.PartnerAccess) ?? "";
                _secrets[KeyVaultSecretNames.ExternalLinks.PartnerAccess] = partnerAccess;

        }

        public Task<byte[]?> GetCertificateBytesAsync(string name, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<string?> GetSecretAsync(string name, CancellationToken ct = default)
       => Task.FromResult(_secrets.TryGetValue(name, out var v) ? v : null);
    }
}
