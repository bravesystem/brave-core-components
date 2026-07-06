using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using BRaVe_Biometric_Matching_Webjob.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using static BRaVe_Biometric_Matching_Webjob.Helpers.KeyVaultSecretNames;

namespace BRaVe_Biometric_Matching_Webjob.Services
{
    public sealed class AzureSecretProvider : ISecretProvider
    {
        private readonly SecretClient _secretClient;

        public AzureSecretProvider()
        {
            var vaultUrl = Environment.GetEnvironmentVariable(SecureStore.Key_Vault);

            if (string.IsNullOrWhiteSpace(vaultUrl))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(vaultUrl));
            }

            _secretClient = new SecretClient(new Uri(vaultUrl), CreateCredential());
        }

        public AzureSecretProvider(SecretClient secretClient)
        {
            _secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
        }

        public string GetSecret(string secretName)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(secretName));
            }

            return _secretClient.GetSecret(secretName).Value.Value;
        }

        public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(secretName));
            }

            var response = await _secretClient
                .GetSecretAsync(secretName, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return response.Value.Value;
        }

        private static TokenCredential CreateCredential()
        {
            if (AzureEncryptionService.IsLocal)
            {
                return new AzureCliCredential();
            }

            return new ManagedIdentityCredential();
            //return new DefaultAzureCredential();
        }
    }
}
