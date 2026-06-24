using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BRaVe_Mobile_Backend.Services
{
    public sealed class JwsSignerService
    {
        private readonly KeyVaultKey _key;
        private readonly ILogger<JwsSignerService> _logger;
        private readonly AzureKeyVaultHelper _kvhelper;
        private readonly string _apiBaseUrl;


        private readonly string _issuer;
        private readonly string _enroll_audience;
        private readonly string _refresh_audience;
        private readonly string _secretKey;


        private readonly CryptographyClient _cryptoClient;


        public JwsSignerService(AzureKeyVaultHelper kvhelper, ILogger<JwsSignerService> logger, ISecretProvider secretProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _kvhelper = kvhelper;

            try
            {
                _logger.LogInformation("Initializing RSA key for JWS signing...");

                _apiBaseUrl = secretProvider.GetSecretAsync(KeyVaultSecretNames.Jwt.API_BASE_URL);

                // Get the RSA key from Key Vault (private key stays in KV)
                _key = _kvhelper.GetKeyAsync().Result;

                _cryptoClient = new CryptographyClient(
                    _key.Id,                         // key identifier (includes version)
                    _kvhelper.CreateCredential());    // however you create TokenCredential

                _logger.LogInformation(
                    "Key Vault key loaded for JWS signing. KeyId: {KeyId}", _key.Key.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing RSA key for JWS signer.");
                throw;
            }
        }

        public string CreateEnrollmentJws(int tenantId, string deviceId, string enrollId, int policyVersion, TimeSpan lifetime, string? jti = null)
        {
            try
            {

                _logger.LogInformation(
                    "Creating enrollment JWS for tenant {TenantId}, policy version {PolicyVersion}, enrollId {EnrollId}",
                    tenantId, policyVersion, enrollId);

                var now = DateTimeOffset.UtcNow;

                // --- same claims you already had ---
                var claims = new Dictionary<string, object>
                {
                    ["iss"] = "https://auth.brave.iom.int",
                    ["aud"] = "brave-enroll",
                    ["jti"] = jti ?? Guid.NewGuid().ToString(),
                    ["iat"] = now.ToUnixTimeSeconds(),
                    ["exp"] = now.Add(lifetime).ToUnixTimeSeconds(),
                    ["tenantId"] = tenantId,
                    ["policyVersion"] = policyVersion,
                    ["apiBaseUrl"] = _apiBaseUrl,
                    ["enrollId"] = enrollId,
                    ["deviceId"] = deviceId, //added later
                    ["useLimit"] = 1
                };

                // --- NEW: build JWT header + payload as JSON ---
                var header = new Dictionary<string, object>
                {
                    ["alg"] = "RS256",
                    ["typ"] = "JWT",
                    ["kid"] = _key.Key.Id   // full KV key id (good for validation side)
                };

                var headerJson = JsonSerializer.Serialize(header);
                var payloadJson = JsonSerializer.Serialize(claims);

                var headerB64 = ByteUtils.ToBase64Url(Encoding.UTF8.GetBytes(headerJson));
                var payloadB64 = ByteUtils.ToBase64Url(Encoding.UTF8.GetBytes(payloadJson));

                var signingInput = $"{headerB64}.{payloadB64}";
                var dataBytes = Encoding.UTF8.GetBytes(signingInput);

                // --- ask Key Vault to sign the bytes with RS256 ---
                var signResult = _cryptoClient.SignData(
                    SignatureAlgorithm.RS256,
                    dataBytes);

                var signatureB64 = ByteUtils.ToBase64Url(signResult.Signature);

                var jws = $"{signingInput}.{signatureB64}";

                _logger.LogInformation(
                    "Successfully created JWS for tenant {TenantId}. Expires at {Expiry}",
                    tenantId, now.Add(lifetime));

                return jws;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to create enrollment JWS for tenant {TenantId}, enrollId {EnrollId}",
                    tenantId, enrollId);
                throw;
            }
        }

        public string getIssuer()
        {
            return _issuer;
        }

        public (string,string) getAudiences()
        {
            return (_enroll_audience, _refresh_audience);
        }


        public string Issue(int tenantId, string deviceId)
        {
            var now = DateTimeOffset.UtcNow;
            var exp = now.AddMinutes(15); // shorter is better

            var nowUnix = now.ToUnixTimeSeconds();
            var expUnix = exp.ToUnixTimeSeconds();

            // 1) Build JWT header
            var header = new Dictionary<string, object>
            {
                ["alg"] = "RS256",
                ["typ"] = "JWT",
                ["kid"] = _key.Key.Id   // full KV key id (helps verifiers)
            };

            // 2) Build JWT payload (claims)
            var payload = new Dictionary<string, object>
            {
                // standard JWT claims
                ["iss"] = "https://auth.brave.iom.int",
                ["aud"] = "brave-api",
                ["jti"] = Guid.NewGuid().ToString(),
                ["iat"] = nowUnix,
                ["nbf"] = nowUnix,
                ["exp"] = expUnix,

                // custom claims
                ["tenantId"] = tenantId,
                ["deviceId"] = deviceId,
                ["sub"] = deviceId
            };

            // 3) Serialize header + payload
            var headerJson = JsonSerializer.Serialize(header);
            var payloadJson = JsonSerializer.Serialize(payload);

            var headerB64 = ByteUtils.ToBase64Url(Encoding.UTF8.GetBytes(headerJson));
            var payloadB64 = ByteUtils.ToBase64Url(Encoding.UTF8.GetBytes(payloadJson));

            var signingInput = $"{headerB64}.{payloadB64}";
            var dataBytes = Encoding.UTF8.GetBytes(signingInput);

            // 4) Ask Key Vault to sign with RS256
            var signResult = _cryptoClient.SignData(SignatureAlgorithm.RS256, dataBytes);
            var signatureB64 = ByteUtils.ToBase64Url(signResult.Signature);

            // 5) Final JWS
            return $"{signingInput}.{signatureB64}";
        }


    }
}
