using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using System.Text.Json;

namespace BRaVe_Portal.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IRestApiService _api;

        public IndexModel(ILogger<IndexModel> logger, IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        public List<UserRoleInfo> UsersWithRoles { get; set; } = new();
        public List<UserRoleInfo> FilteredUsers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            _logger.LogInformation("Users Index page accessed");

            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            _logger.LogInformation("Resolved language code: {LanguageCode}", languageCode);

            string url = $"/api/v1/AccessControl/assignments?languageCode={languageCode}";
            _logger.LogInformation("Base API URL constructed: {Url}", url);

            int tenantId = User.Tenant();
            _logger.LogInformation("Resolved TenantId: {TenantId}", tenantId);

            if (User.IsInRole(EnumUserRoles.PA.ToString()))
            {
                url += $"&tenantId={tenantId}";
                _logger.LogInformation("User is PA. Tenant filter applied to API URL: {Url}", url);
            }

            try
            {
                _logger.LogInformation("Calling AccessControl assignments API");
                UsersWithRoles = await _api.GetAsync<List<UserRoleInfo>>(url) ?? new List<UserRoleInfo>();
                _logger.LogInformation("API call successful. Records returned: {Count}", UsersWithRoles.Count);

                if (User.IsInRole(EnumUserRoles.PA.ToString()))
                {
                    _logger.LogInformation("User role PA detected. Redirecting to PAUserList");
                    TempData["UsersWithRoles"] = JsonSerializer.Serialize(UsersWithRoles);
                    return RedirectToPage("./PAUserList");
                }
                else if (User.IsInRole(EnumUserRoles.Admin.ToString()))
                {
                    _logger.LogInformation("User role Admin detected. Redirecting to AdminUserList");
                    TempData["UsersWithRoles"] = JsonSerializer.Serialize(UsersWithRoles);
                    return RedirectToPage("./AdminUserList");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading users with roles");
                UsersWithRoles = new List<UserRoleInfo>();
            }

            _logger.LogInformation("Rendering Users Index page");
            return Page();
        }
    }
}
