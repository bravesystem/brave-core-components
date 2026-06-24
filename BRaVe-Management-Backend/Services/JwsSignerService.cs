using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using BRaVe_Management_Backend.Helpers;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace BRaVe_Management_Backend.Services
{
    public sealed class JwsSignerService
    {
        private readonly KeyVaultKey _key;
        private readonly ILogger<JwsSignerService> _logger;
        private readonly AzureKeyVaultHelper _kvhelper;

        private readonly CryptographyClient _cryptoClient;

        public JwsSignerService(AzureKeyVaultHelper kvhelper, ILogger<JwsSignerService> logger)
        {
                
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            try
            {
                _logger.LogInformation("Initializing RSA key for JWS signing...");

                _kvhelper = kvhelper;

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

        public string CreateEnrollmentJws(int tenantId, int policyVersion, string enrollId, Uri apiBaseUrl, TimeSpan lifetime, string? jti = null,string? deviceId=null)
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
                    ["apiBaseUrl"] = apiBaseUrl.ToString(),
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

                var headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
                var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

                var signingInput = $"{headerB64}.{payloadB64}";
                var dataBytes = Encoding.UTF8.GetBytes(signingInput);

                // --- ask Key Vault to sign the bytes with RS256 ---
                var signResult = _cryptoClient.SignData(
                    SignatureAlgorithm.RS256,
                    dataBytes);

                var signatureB64 = Base64UrlEncode(signResult.Signature);

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

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
