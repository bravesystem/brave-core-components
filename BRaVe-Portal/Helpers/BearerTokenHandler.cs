using BRaVe_Portal.Helpers.Mock;
using BRaVe_Portal.Interfaces;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace BRaVe_Portal.Helpers
{
    public class BearerTokenHandler : DelegatingHandler
    {
        public const string FormAuthMethodClaim = "auth_method";
        public const string FormAuthMethodValue = "form";

        private readonly IHttpContextAccessor _ctx;
        private readonly ILogger<BearerTokenHandler> _logger;
        private readonly ITokenAcquisition? _tokenAcquisition;
        private readonly ISecretProvider _secretProvider;
        private string? _scopes;

        public BearerTokenHandler(
            ISecretProvider secretProvider,
            IHttpContextAccessor ctx,
            IServiceProvider serviceProvider,
            ILogger<BearerTokenHandler> logger)
        {
            _ctx = ctx;
            _logger = logger;
            _tokenAcquisition = serviceProvider.GetService<ITokenAcquisition>();
            _secretProvider = secretProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {


            try
            {
                var path = request.RequestUri?.AbsolutePath ?? "";
                ClaimsPrincipal? user = _ctx.HttpContext?.User;

                // FormAccess endpoints never use MSAL; authenticated form users send identity headers only.
                if (path.Contains("/api/v1/FormAccess/", StringComparison.OrdinalIgnoreCase))
                {
                    if (user?.Identity?.IsAuthenticated == true && IsFormAuthUser(user))
                        AddFormUserHeaders(request, user);

                    return await base.SendAsync(request, cancellationToken);
                }

                if (user == null || !user.Identity.IsAuthenticated)
                    throw new UnauthorizedAccessException("User is not authenticated.");

                var isAadUser = IsAadUser(user);

                // Form users: identity headers only — no MSAL, no Fe_Audience_Scope required.
                if (!isAadUser)
                {
                    AddFormUserHeaders(request, user);
                    _logger.LogDebug("BearerTokenHandler attached form-auth headers for {Path}", path);
                    return await base.SendAsync(request, cancellationToken);
                }

                if (_tokenAcquisition == null)
                    throw new InvalidOperationException("Entra token acquisition is not configured (form-only auth mode).");

                var scopes = (await GetScopesAsync())
                    .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                string? token = await _tokenAcquisition.GetAccessTokenForUserAsync(
                    scopes, OpenIdConnectDefaults.AuthenticationScheme, null, null, user, null);

                var tenant = user.FindFirstValue("Tenant") ?? "0";
                var rolesCsv = string.Join(",", user.FindAll(ClaimTypes.Role).Select(c => c.Value));
                request.Headers.Add("X-User-Roles", rolesCsv);
                request.Headers.Add("X-Tenant", tenant);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                _logger.LogDebug(
                    "BearerTokenHandler attached token for {Path}, isAadUser={IsAadUser}, Tenant={Tenant}",
                    path, isAadUser, tenant);

                return await base.SendAsync(request, cancellationToken);

            }
            catch (Microsoft.Identity.Web.MicrosoftIdentityWebChallengeUserException ex)
            {
                _logger.LogWarning(ex,
                    "Token acquisition failed for {Path} (challenge required). Inner: {Inner}",
                    request.RequestUri?.AbsolutePath,
                    ex.InnerException?.Message);
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }
            catch (Microsoft.Identity.Client.MsalUiRequiredException ex)
            {
                _logger.LogWarning(ex,
                    "Token acquisition failed for {Path} (MSAL UI required). isFormUser={IsFormUser}. Inner: {Inner}",
                    request.RequestUri?.AbsolutePath,
                    IsFormAuthUser(_ctx.HttpContext?.User),
                    ex.InnerException?.Message);
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }
            catch (Microsoft.Identity.Client.MsalException ex)
            {
                _logger.LogWarning(ex,
                    "Token acquisition failed for {Path}. ErrorCode={ErrorCode}. Inner: {Inner}",
                    request.RequestUri?.AbsolutePath,
                    ex.ErrorCode,
                    ex.InnerException?.Message);
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            }


        }

        private async Task<string> GetScopesAsync()
        {
            if (_scopes != null)
                return _scopes;

            _scopes = await _secretProvider.GetSecretAsync(KeyVaultSecretNames.AzureAd.Fe_Audience_Scope)
                ?? throw new InvalidOperationException(
                    $"{KeyVaultSecretNames.AzureAd.Fe_Audience_Scope} is required for Entra user API calls.");

            return _scopes;
        }

        public static bool IsFormAuthUser(ClaimsPrincipal? user) =>
            user?.HasClaim(FormAuthMethodClaim, FormAuthMethodValue) == true
            || user?.FindFirst(ClaimTypes.NameIdentifier)?.Value?.StartsWith("FA-", StringComparison.OrdinalIgnoreCase) == true;

        private static bool IsAadUser(ClaimsPrincipal user)
        {
            if (IsFormAuthUser(user))
                return false;

            var utid = user.FindFirst("utid")?.Value;
            var tenantId = Environment.GetEnvironmentVariable(KeyVaultSecretNames.AzureAd.Fe_Tenant_Id);

            return !string.IsNullOrEmpty(utid)
                && !string.IsNullOrEmpty(tenantId)
                && string.Equals(utid, tenantId, StringComparison.OrdinalIgnoreCase);
        }

        private static void AddFormUserHeaders(HttpRequestMessage request, ClaimsPrincipal user)
        {
            var tenant = user.FindFirstValue("Tenant") ?? "0";
            var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = user.FindFirst(ClaimTypes.Email)?.Value;
            var name = user.FindFirst(ClaimTypes.Name)?.Value;
            var rolesCsv = string.Join(",", user.FindAll(ClaimTypes.Role).Select(c => c.Value));

            request.Headers.Add("X-User-Roles", rolesCsv);
            request.Headers.Add("X-Tenant", tenant);
            request.Headers.Add("X-Nonce", Guid.NewGuid().ToString());
            request.Headers.Add("X-UserId", nameIdentifier);
            request.Headers.Add("X-UserName", name);
            request.Headers.Add("X-UserEmail", email);
        }


    }
}
