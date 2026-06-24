using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using static BRaVe_Mobile_Backend.Helpers.KeyVaultSecretNames;

namespace BRaVe_Mobile_Backend.Services
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

            string vault_url = secret.GetSecretAsync(SecureStore.Key_Vault);

            _pubKeyName = secret.GetSecretAsync(SecureStore.Pub_KeyName);

            var credential =  CreateCredential();

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

        /*public AzureKeyVaultHelper(IConfiguration configuration, ISecretProvider secret) 
        {

            string vault_url = secret.GetSecretAsync(KeyVaultSecretNames.SecureStore.Key_Vault);

            _pubKeyName = secret.GetSecretAsync(KeyVaultSecretNames.SecureStore.Pub_KeyName);
            
            var tenantId = configuration["AzureAd:TenantId"];
            var clientId = configuration["AzureAd:ClientId"];
            var clientSecret = configuration["AzureAd:ClientSecret"];
            
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

            // Create KeyClient
            _keyClient = new KeyClient(new Uri(vault_url), credential);

        }*/

        public async Task<KeyVaultKey> GetKeyAsync()
        {
            if (_key == null)
            {
                _key = await _keyClient.GetKeyAsync(_pubKeyName);
            }

            return _key;
        }

        public async Task<string> GetRsaPubKeyB64()
        {
            if( _key == null )
            {
                // Get the key
                _key = await _keyClient.GetKeyAsync(_pubKeyName);
            }

            //return Convert.ToBase64String(_key.Key.N);

            var jwk = _key.Key;

            if (jwk.N == null || jwk.E == null)
                throw new Exception("Key is not RSA or missing parameters");

            using var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = jwk.N,
                Exponent = jwk.E
            });

            // Export as SubjectPublicKeyInfo (X.509)
            byte[] spki = rsa.ExportSubjectPublicKeyInfo();
            string base64 = Convert.ToBase64String(spki);
            return base64;
        }

        public async Task<KeyVaultKey> GetKeyAsync(string keyName)
        { 
            return await _keyClient.GetKeyAsync(keyName);
        }


    }
}
