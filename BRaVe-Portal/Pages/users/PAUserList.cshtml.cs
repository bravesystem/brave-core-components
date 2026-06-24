using BRaVe_Portal.Extensions;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace BRaVe_Portal.Pages.Users
{
    public class PAUserListModel : PageModel
    {
        private readonly IRestApiService _api;
        private readonly ILookupService _lookupService;

        public PAUserListModel(IRestApiService api, ILookupService lookupService)
        {
            _api = api;
            _lookupService = lookupService;
        }


        [BindProperty]
        public string SelectedUserId { get; set; }

        [BindProperty]
        public List<string> SelectedRoleIds { get; set; } = new List<string>();

        [TempData]
        public string? SuccessMessage { get; set; }
        public List<UserRoleInfo> UsersWithRoles { get; set; } = new();
        public List<UserRoleInfo> FilteredUsers { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty]
        public string ExistingRoleIdsRaw { get; set; } = string.Empty;

 
        public List<string> ExistingRoleIds
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ExistingRoleIdsRaw))
                    return new List<string>();

                return ExistingRoleIdsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            }
        }
        [ValidateAntiForgeryToken]
        public async Task OnGetAsync()
        {

            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);
            ViewData["CoreLookups"] = coreLookups;
            // Fetch all PA users from API every GET request
            UsersWithRoles = await _api.GetAsync<List<UserRoleInfo>>("/api/v1/AccessControl/assignments?tenantId=" + User.Tenant())
                             ?? new List<UserRoleInfo>();

            // Apply search
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                CurrentPage = 1; // reset to first page for new search
                UsersWithRoles = UsersWithRoles
                    .Where(u => u.UserId.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                             || u.ProfileName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                             || u.Roles.Any(r => r.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            TotalRecords = UsersWithRoles.Count;

            // Apply paging
            FilteredUsers = UsersWithRoles
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (string.IsNullOrEmpty(SelectedUserId))
            {
                ModelState.AddModelError(string.Empty, "No user selected.");
                await OnGetAsync();
                return Page();
            }

            SelectedRoleIds ??= new List<string>();
              

            try
            {
                int tenantId = User.Tenant();
                string currentUserId = User.Identifier();

                // Get current user roles from API
                var allAssignments = await _api.GetAsync<List<UserRoleInfo>>("/api/v1/AccessControl/assignments?tenantId=" + User.Tenant())
                             ?? new List<UserRoleInfo>();
                var existingUser = allAssignments?.FirstOrDefault(u => u.UserId == SelectedUserId);
                var existingRoles =  ExistingRoleIds ?? new List<string>();

                // Determine roles to remove and add
                var rolesToRemove = existingRoles.Except(SelectedRoleIds).ToList();
                var rolesToAdd = SelectedRoleIds.Except(existingRoles).ToList();

                // Call remove-roles endpoint if needed
                if (rolesToRemove.Any())
                {
                    string removeRoleIds = string.Join(",", rolesToRemove);
                    string removeUrl = $"v1/AccessControl/remove-roles?userId={Uri.EscapeDataString(SelectedUserId)}" +
                                       $"&tenantId={tenantId}" +
                                       $"&roleIds={Uri.EscapeDataString(removeRoleIds)}" +
                                       $"&deletedByUserId={Uri.EscapeDataString(currentUserId)}";

                    await _api.DeleteAsync(removeUrl, ct);
                }

                // Call assign-roles endpoint if needed
                if (rolesToAdd.Any())
                {
                    string addRoleIds = string.Join(",", rolesToAdd);
                    string addUrl = $"v1/AccessControl/assign-roles?userId={Uri.EscapeDataString(SelectedUserId)}" +
                                    $"&tenantId={tenantId}" +
                                    $"&roleIds={Uri.EscapeDataString(addRoleIds)}" +
                                    $"&createdByUserId={Uri.EscapeDataString(currentUserId)}";

                    // PostJsonAsync requires a generic request body — send an empty object
                    await _api.PostJsonAsync<object, object>(addUrl, new { }, ct);
                }

                SuccessMessage = "User roles updated successfully!";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error updating user roles: {ex.Message}");
            }

            await OnGetAsync();
            return RedirectToPage();
        }




    }
}
