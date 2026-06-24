using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using static System.Net.WebRequestMethods;

namespace BRaVe_Portal.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _cfg;
        private readonly IAccessControlClient _access;
        private readonly IHttpContextAccessor _httpCtx;

        private readonly IAuthModeService _authModeService;

        public AccountController(IConfiguration cfg, IAuthModeService authModeService, IAccessControlClient access, IHttpContextAccessor httpCtx)
        {
            _cfg = cfg;
            _access = access;
            _httpCtx = httpCtx;
            _authModeService = authModeService;
        }


        [HttpGet("/auth-login")]
        public async Task<IActionResult> AzureADLogin(CancellationToken ct)
        {

            //if (_authModeService.IsMockMode()) { 
            //    return await DevLogin(ct);
            //}

            // If user is not authenticated, challenge Azure AD
            var result = await HttpContext.AuthenticateAsync(OpenIdConnectDefaults.AuthenticationScheme);
            
            if (!result.Succeeded)
            {
                return Challenge(OpenIdConnectDefaults.AuthenticationScheme);
            }

            // Extract info from Azure AD claims
            var azureClaims = result.Principal.Claims;

            var userId = azureClaims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var name = azureClaims.FirstOrDefault(c => c.Type == "name")?.Value ?? "Guest";
            var email = azureClaims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                        ?? azureClaims.FirstOrDefault(c => c.Type == "preferred_username")?.Value;

            return Redirect("~/");

        }


        [HttpGet("/logout")]
        public async Task<IActionResult> DevLogout(CancellationToken ct) {

            //Update the logout time for temporary access requests
            var userId = User.Identity?.Name;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                try
                {
                    await _access.UpdateLogoutTimeAsync(ct);
                }
                catch
                {
                    // ignore errors — still log out
                }
            }

            // Form-auth users: only signed in with Cookies (no OIDC). AAD users: signed in with Cookies + OIDC.
            var isAadUser = User.Identity?.IsAuthenticated == true
                && !string.IsNullOrEmpty(User.FindFirst("utid")?.Value);

            if (isAadUser)
            {
                var props = new AuthenticationProperties
                {
                    RedirectUri = Url.Content("~/")
                };
                return SignOut(props,
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    OpenIdConnectDefaults.AuthenticationScheme);
            }

            // Form-auth: sign out cookie only, then redirect to home
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("~/");

        }
    }
}
