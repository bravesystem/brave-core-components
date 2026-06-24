using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using System.Globalization;

namespace BRaVe_Portal.Pages.requests
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IRestApiService _api;
        private readonly ILookupService _lookupService;

        public IndexModel(ILogger<IndexModel> logger, IRestApiService api, ILookupService lookupService)
        {
            _logger = logger;
            _api = api;
            _lookupService = lookupService;
        }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        // Store filtered list for paging
        public List<UserMissionRequest> FilteredRequests { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public List<UserMissionRequest> PendingUserRequests { get; set; } = new();

        [BindProperty]
        public int? SelectedUser { get; set; }

        [BindProperty]
        public int? SelectedMissionId { get; set; }

        [BindProperty]
        public int? SelectedRoleId { get; set; }

        public List<SelectListItem> Missions { get; set; } = new();
        public List<SelectListItem> Roles { get; set; } = new();

        [BindProperty]
        public string? SuccessMessage { get; set; }

        public async Task OnGetAsync()
        {
            _logger.LogInformation("Loading pending user requests and lookup data.");

            try
            {
                // Fetch pending user requests
                PendingUserRequests = await _api.GetAsync<List<UserMissionRequest>>("v1/MissionAccesses")
                                       ?? new List<UserMissionRequest>();

                _logger.LogInformation("Fetched {Count} pending requests.", PendingUserRequests.Count);

                // Filter based on search term
                var query = string.IsNullOrWhiteSpace(SearchTerm)
                    ? PendingUserRequests
                    : PendingUserRequests
                        .Where(u => u.UserId.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                                 || u.ProfileName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                                 || u.Justification.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                // Count BEFORE paging
                TotalRecords = query.Count;

                // Apply paging
                FilteredRequests = query
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                _logger.LogInformation("Filtered requests count after search and paging: {Count}", FilteredRequests.Count);

                // Fetch missions
                var missions = await _api.GetAsync<List<Mission>>("v1/missions") ?? new List<Mission>();
                Missions = missions.Select(m => new SelectListItem
                {
                    Value = m.MissionId.ToString(),
                    Text = m.Name
                }).ToList();

                // Fetch roles
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                var coreLookups = await _lookupService.GetCoreLookups(languageCode);

                if (coreLookups?.Roles != null)
                {
                    Roles = coreLookups.Roles
                        .Select(r => new SelectListItem
                        {
                            Value = r.Id.ToString(),
                            Text = r.Text
                        })
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading pending requests or lookup data.");
                throw;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Assigning mission to user. SelectedUser={UserId}, SelectedMissionId={MissionId}, SelectedRoleId={RoleId}",
                SelectedUser, SelectedMissionId, SelectedRoleId);

            if (SelectedUser == null || SelectedMissionId == null || SelectedRoleId == null)
            {
                ModelState.AddModelError(string.Empty, "Please select a user, a mission, and a role.");
                _logger.LogWarning("Missing user, mission, or role selection.");
                await OnGetAsync(); // Reload dropdowns and requests
                return Page();
            }

            var payload = new UserMissionResultDto
            {
                RequestId = SelectedUser.Value,
                MissionId = SelectedMissionId.Value,
                RoleId = SelectedRoleId.Value
            };

            try
            {
                await _api.PostJsonAsync<UserMissionResultDto, object>("v1/MissionAccesses/assign", payload);
                SuccessMessage = "User successfully assigned!";
                _logger.LogInformation("User {UserId} assigned to Mission {MissionId} with Role {RoleId}",
                    SelectedUser, SelectedMissionId, SelectedRoleId);

                // Reset dropdowns
                SelectedMissionId = null;
                SelectedRoleId = null;
                ModelState.Remove(nameof(SelectedMissionId));
                ModelState.Remove(nameof(SelectedRoleId));
            }
            catch (Exception e)
            {
                string message = "Could not assign mission to user.";

                if (e.Message.Contains("409 conflict", StringComparison.CurrentCultureIgnoreCase))
                {
                    message = "This user has already been assigned to a mission.";
                    _logger.LogWarning(e, "Conflict while assigning mission to user {UserId}", SelectedUser);
                }
                else
                {
                    _logger.LogError(e, "Error assigning mission to user {UserId}", SelectedUser);
                }

                ModelState.AddModelError(string.Empty, message);
            }

            // Reload dropdowns and requests
            await OnGetAsync();

            return Page();
        }
    }
}
