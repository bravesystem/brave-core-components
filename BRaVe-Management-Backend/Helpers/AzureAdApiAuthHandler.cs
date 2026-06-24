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

                // Note: NO /v2.0 here → uses v1 metadata
                var metadataUrl = $"{aad_instance}{aad_tenant}/.well-known/openid-configuration";

                var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    metadataUrl,
                    new OpenIdConnectConfigurationRetriever());

                var openIdConfig = await configManager.GetConfigurationAsync(CancellationToken.None);

                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = openIdConfig.Issuer,
                    ValidAudience = appIdUri,
                    IssuerSigningKeys = openIdConfig.SigningKeys
                };

                var principal = handler.ValidateToken(token, validationParams, out _);

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
                _logger.LogError(ex, "Token validation failed");
                return AuthenticateResult.Fail("Invalid token");
            }
        }
    }
}
