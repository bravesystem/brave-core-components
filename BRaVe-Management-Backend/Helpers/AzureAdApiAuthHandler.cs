using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace BRaVe_Management_Backend.Helpers
{
    public class AzureAdApiAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AzureAdApiAuthHandler> _logger;
        private readonly bool _isPartnerEnvironment;

        public AzureAdApiAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> o,
            IConfiguration config,
            ILogger<AzureAdApiAuthHandler> logger,
            ILoggerFactory loggerFactory,
            UrlEncoder urlEncoder,
            ISystemClock clock) : base(o, loggerFactory, urlEncoder, clock)
        {
            _config = config;
            _logger = logger;
            _isPartnerEnvironment = string.Equals(
                Environment.GetEnvironmentVariable("Environment"),
                "PARTNER",
                StringComparison.OrdinalIgnoreCase);
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var userId = Request.Headers["X-UserId"].FirstOrDefault();
            var nonce = Request.Headers["X-Nonce"].FirstOrDefault();
            var isFormAuthRequest = !string.IsNullOrWhiteSpace(nonce)
                || userId?.StartsWith("FA-", StringComparison.OrdinalIgnoreCase) == true;

            if (isFormAuthRequest)
                return Task.FromResult(AuthenticateFormUser());

            var authHeader = Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug(
                    "No authentication provided for {Path}. Form auth requires X-Nonce or FA- user id; Entra auth requires Bearer token.",
                    Request.Path);
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            return AuthenticateBearerAsync(authHeader);
        }

        private AuthenticateResult AuthenticateFormUser()
        {
            var userId = Request.Headers["X-UserId"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Form auth rejected: missing X-UserId.");
                return AuthenticateResult.Fail("Missing X-UserId for form authentication.");
            }

            if (_isPartnerEnvironment &&
                !userId.StartsWith("FA-", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Form auth rejected in PARTNER environment: UserId {UserId} is not a form-auth id.", userId);
                return AuthenticateResult.Fail("Invalid form authentication user id.");
            }

            var tenantId = Request.Headers["X-Tenant"].FirstOrDefault() ?? "0";
            var roles = Request.Headers["X-User-Roles"].ToString();
            var name = Request.Headers["X-UserName"].FirstOrDefault() ?? string.Empty;
            var email = Request.Headers["X-UserEmail"].FirstOrDefault() ?? string.Empty;

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Email, email),
                new(ClaimTypes.Name, name),
                new("Tenant", tenantId),
                new("auth_method", "form"),
            };

            foreach (var role in roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                claims.Add(new Claim(ClaimTypes.Role, role));

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            _logger.LogInformation(
                "Form auth accepted: UserId={UserId}, Email={Email}, Tenant={Tenant}, PartnerMode={PartnerMode}",
                userId, email, tenantId, _isPartnerEnvironment);

            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
        }

        private async Task<AuthenticateResult> AuthenticateBearerAsync(string authHeader)
        {
            var tenantId = Request.Headers["X-Tenant"].ToString();
            var roles = Request.Headers["X-User-Roles"].ToString();
            var token = authHeader["Bearer ".Length..].Trim();

            try
            {
                var handler = new JwtSecurityTokenHandler();

                string? aad_client = Environment.GetEnvironmentVariable(KeyVaultSecretNames.AzureAd.Be_Client_Id);
                string? aad_instance = Environment.GetEnvironmentVariable(KeyVaultSecretNames.AzureAd.Be_Instance);
                string? aad_tenant = Environment.GetEnvironmentVariable(KeyVaultSecretNames.AzureAd.Be_Tenant_Id);

                if (string.IsNullOrWhiteSpace(aad_client) ||
                    string.IsNullOrWhiteSpace(aad_instance) ||
                    string.IsNullOrWhiteSpace(aad_tenant))
                {
                    _logger.LogError("Bearer auth misconfigured: Be_Client_Id, Be_Instance, or Be_Tenant_Id is missing.");
                    return AuthenticateResult.Fail("Bearer authentication is not configured.");
                }

                var appIdUri = $"api://{aad_client}";
                var useV2Metadata = UseV2OidcMetadata(aad_instance, aad_tenant);
                var metadataUrl = BuildMetadataUrl(aad_instance, aad_tenant, useV2Metadata);

                _logger.LogDebug(
                    "Validating Bearer token. PartnerMode={PartnerMode}, V2Metadata={UseV2Metadata}, MetadataUrl={MetadataUrl}",
                    _isPartnerEnvironment, useV2Metadata, metadataUrl);

                var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    metadataUrl,
                    new OpenIdConnectConfigurationRetriever());

                var openIdConfig = await configManager.GetConfigurationAsync(CancellationToken.None);

                // v2 access tokens may use api://{id} or bare GUID as aud; iss may be v1 or v2 format.
                var validIssuers = BuildValidIssuers(aad_instance, aad_tenant, openIdConfig.Issuer, useV2Metadata);
                var validAudiences = new[] { appIdUri, aad_client };

                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers = validIssuers,
                    ValidateAudience = true,
                    ValidAudiences = validAudiences,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = openIdConfig.SigningKeys
                };

                var principal = handler.ValidateToken(token, validationParams, out var validatedToken);

                _logger.LogDebug(
                    "Bearer token validated. Issuer={Issuer}, Audience={Audience}, V2Metadata={UseV2Metadata}",
                    validatedToken is JwtSecurityToken jwt ? jwt.Issuer : "(unknown)",
                    validatedToken is JwtSecurityToken jwt2 ? string.Join(",", jwt2.Audiences) : "(unknown)",
                    useV2Metadata);

                var userId = principal.Identifier();
                var email = principal.GetUserEmailLike();
                var name = principal.DisplayName();

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, userId),
                    new(ClaimTypes.Email, email),
                    new(ClaimTypes.Name, name),
                    new("Tenant", tenantId)
                };

                foreach (var role in roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    claims.Add(new Claim(ClaimTypes.Role, role));

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Token validation failed. PartnerMode={PartnerMode}, Path={Path}",
                    _isPartnerEnvironment, Request.Path);
                return AuthenticateResult.Fail("Invalid token");
            }
        }

        private bool UseV2OidcMetadata(string instance, string tenantId)
        {
            if (_isPartnerEnvironment)
                return true;

            if (string.Equals(
                    Environment.GetEnvironmentVariable("Be_Oidc_MetadataVersion"),
                    "v2",
                    StringComparison.OrdinalIgnoreCase))
                return true;

            if (instance.Contains("ciamlogin.com", StringComparison.OrdinalIgnoreCase))
                return true;

            return tenantId is "common" or "organizations" or "consumers";
        }

        private static string BuildMetadataUrl(string instance, string tenantId, bool useV2Metadata)
        {
            var baseUrl = instance.EndsWith('/') ? instance : instance + "/";
            return useV2Metadata
                ? $"{baseUrl}{tenantId}/v2.0/.well-known/openid-configuration"
                : $"{baseUrl}{tenantId}/.well-known/openid-configuration";
        }

        private static string[] BuildValidIssuers(
            string instance,
            string tenantId,
            string? metadataIssuer,
            bool useV2Metadata)
        {
            var issuers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(metadataIssuer))
                issuers.Add(metadataIssuer);

            var baseUrl = instance.TrimEnd('/');
            issuers.Add($"{baseUrl}/{tenantId}/");
            issuers.Add($"{baseUrl}/{tenantId}/v2.0");

            // v1 sts format (legacy tokens from same tenant)
            if (baseUrl.Contains("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase))
                issuers.Add($"https://sts.windows.net/{tenantId}/");

            if (useV2Metadata)
                issuers.Add($"https://login.microsoftonline.com/{tenantId}/v2.0");

            return issuers.ToArray();
        }
    }
}
