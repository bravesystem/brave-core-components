using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace BRaVe_Portal.Pages.Users
{
    public class AdminUserListModel : PageModel
    {
        private readonly IRestApiService _api;

        public AdminUserListModel(IRestApiService api)
        {
            _api = api;
        }

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

        public async Task OnGetAsync()
        {
            // Fetch all users every GET
            UsersWithRoles = await _api.GetAsync<List<UserRoleInfo>>("/api/v1/AccessControl/assignments")
                             ?? new List<UserRoleInfo>();

            // Filter by search term
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                CurrentPage = 1; // reset page on search
                UsersWithRoles = UsersWithRoles
                    .Where(u => u.UserId.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                             || u.MissionName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
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
    }

}