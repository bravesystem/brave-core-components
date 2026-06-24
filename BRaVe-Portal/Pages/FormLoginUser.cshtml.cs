using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Security.Claims;
using System.Security.Principal;

namespace BRaVe_Portal.Pages
{
    [AllowAnonymous]
    public class FormLoginUserModel : PageModel
    {
        private readonly ILogger<FormLoginUserModel> _logger;
        private readonly IRestApiService _api;
        private readonly IAccessControlClient _access;

        public FormLoginUserModel(
            ILogger<FormLoginUserModel> logger,
            IRestApiService api,
            IAccessControlClient access)
        {
            _logger = logger;
            _api = api;
            _access = access;
        }

        [BindProperty]
        public UatFormLoginDto Dto { get; set; } = new UatFormLoginDto();

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            if (TempData.TryGetValue("ErrorMessage", out var msg) && msg is string error)
                ErrorMessage = error;

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostLoginAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid login information.";
                return RedirectToPage();
            }

            try
            {
                // 1. Call backend to validate credentials / record login
                UatFormAuthResult res =
                await _api.PostJsonAsync<UatFormLoginDto, UatFormAuthResult>("v1/FormAccess/login", Dto);

                var userId = $"FA-{res.UserId}"; // using email as stable identifier in UAT
                var email = res.Email;
                var name = res.DisplayName;

                // 2. Minimal identity
                var minimalIdentity = new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId),
                        new Claim(ClaimTypes.Name, name),
                        new Claim(ClaimTypes.Email, email),
                    },
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    ClaimTypes.Name,
                    ClaimTypes.Role);

                var principal = new ClaimsPrincipal(minimalIdentity);
                HttpContext.User = principal;

                // 3. Call backend for roles/tenant
                TenantAndRolesDto tenantAndRoles;
                var roleList = new List<string>();

                UserRoleInfo userRoleInfo = null;
                string TenantId = "0";

                try
                {
                    tenantAndRoles = await _access.GetFormAuthRolesAsync(userId!, "en", ct);

                    TenantId = tenantAndRoles.TenantToString();

                    userRoleInfo = tenantAndRoles.UserProfile;
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    roleList.Add(EnumUserRoles.NoAuth.ToString());
                    tenantAndRoles = new TenantAndRolesDto();
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw;
                }

                foreach (var role in tenantAndRoles.Roles)
                {
                    roleList.Add(EnumHelper.ToString<EnumUserRoles>(role, EnumUserRoles.NoAuth.ToString()));
                }

                string rolesText = "No Roles";
                string missionName = "No Mission Assigned";

                if (userRoleInfo != null)
                {
                    // Mission: backend already computes the correct mission for this user
                    missionName = userRoleInfo.MissionName?.Trim();

                    // Roles: backend already resolves role names (including temp override)

                    var allRoles = userRoleInfo.Roles
                        .Where(r => !string.IsNullOrWhiteSpace(r))
                        .Distinct()
                        .ToList();

                    rolesText = string.Join(", ", allRoles);
                }

                minimalIdentity.AddClaim(new Claim(
                    type: "Mission",
                    value: missionName,
                    valueType: ClaimValueTypes.String));

                minimalIdentity.AddClaim(new Claim(
                    type: "AllRoles",
                    value: rolesText,
                    valueType: ClaimValueTypes.String));

                minimalIdentity.AddClaim(new Claim(
                    type: "Tenant",
                    value: tenantAndRoles.TenantToString(),
                    valueType: ClaimValueTypes.Integer32
                ));

                // 4. Re-issue cookie with roles
                var fullClaims = minimalIdentity.Claims.Concat(
                    roleList.Select(r => new Claim(ClaimTypes.Role, r)));

                var fullIdentity = new ClaimsIdentity(
                    fullClaims,
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    ClaimTypes.Name,
                    ClaimTypes.Role);

                principal = new ClaimsPrincipal(fullIdentity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true
                    });

                return Redirect("~/");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Form login failed for {Email}", Dto.Email);
                TempData["ErrorMessage"] = "Login failed. Please check your credentials or try again later.";
                return RedirectToPage();
            }
        }
    }
}
