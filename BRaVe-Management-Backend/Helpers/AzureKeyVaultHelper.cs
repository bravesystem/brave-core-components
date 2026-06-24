using Azure.Identity;
using BRaVe_Management_Backend.Interfaces;
using System.Net;
using Azure.Security.KeyVault.Keys;
using Azure.Core;

namespace BRaVe_Management_Backend.Helpers
{
    public class AzureKeyVaultHelper
    {
        private KeyClient _keyClient;
        private string _pubKeyName;
        private KeyVaultKey _key;

        private readonly IAuthModeService _authModeService;

        public AzureKeyVaultHelper(IAuthModeService authModeService, ISecretProvider secret)
        {
            _authModeService = authModeService;

            string vault_url = secret.GetSecretAsync(KeyVaultSecretNames.SecureStore.Key_Vault).Result;

            _pubKeyName = secret.GetSecretAsync(KeyVaultSecretNames.SecureStore.Pub_KeyName).Result;

            var credential = CreateCredential(); //new DefaultAzureCredential();

            // Create KeyClient
            _keyClient = new KeyClient(new Uri(vault_url), credential);

        }

        public TokenCredential CreateCredential()
        {
            if (_authModeService.IsMockMode())
            {
                return new AzureCliCredential();
            }

            return new DefaultAzureCredential();
        }


        public async Task<KeyVaultKey> GetKeyAsync()
        {
            if (_key == null)
            {
                // Get the key
                _key = await _keyClient.GetKeyAsync(_pubKeyName);
            }

            return _key;
        }

        public async Task<KeyVaultKey> GetKeyAsync(string keyName)
        {
            return await _keyClient.GetKeyAsync(keyName);
        }

    }
}
