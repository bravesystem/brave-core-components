using BRaVe_Management_Backend.DTOs;
using BRaVe_Portal.Extensions;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

namespace BRaVe_Portal.Pages.setup.UserMissionAssignment
{
    [Authorize(Roles = "Admin,PA")]
    public class IndexModel : PageModel
    {
        private readonly IRestApiService _api;
        private readonly ILookupService _lookupService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IRestApiService api,
            ILookupService lookupService,
            ILogger<IndexModel> logger)
        {
            _api = api;
            _lookupService = lookupService;
            _logger = logger;
        }

        /* ===== FORM DATA ===== */

        [BindProperty]
        public int SelectedMissionId { get; set; }

        [BindProperty]
        public List<MissionPreAssignmentDto> Assignments { get; set; } = new();

        /* ===== DROPDOWNS ===== */

        public List<SelectListItem> Missions { get; set; } = new();
        public List<SelectListItem> Roles { get; set; } = new();
        public List<PreAssignedUserDto> PendingUsers { get; set; } = new();


        private readonly string _languageCode =
            CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        public async Task OnGetAsync()
        {
            Missions = (await _api.GetAsync<List<Mission>>("v1/missions") ?? [])
                .Select(m => new SelectListItem
                {
                    Value = m.MissionId.ToString(),
                    Text = m.Name
                }).ToList();

            if (User.Tenant() > 0)
            {
                Missions = Missions
                    .Where(x => x.Value == User.Tenant().ToString())
                    .ToList();
            }

            if (Missions.Count == 1)
            {
                SelectedMissionId = int.Parse(Missions.First().Value);
            }



            var coreLookups = await _lookupService.GetCoreLookups(_languageCode);

            Roles = coreLookups?.Roles?
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Text
                })
                .ToList() ?? new();

            // Load pending pre-assigned users
            PendingUsers =
                (await _api.GetAsync<List<PreAssignedUserDto>>(
                    "v1/MissionAccesses/pre-assign/pending")) ?? new();

        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            if (SelectedMissionId <= 0 || Assignments.Count == 0)
            {
                ModelState.AddModelError("", "Invalid submission.");
                await OnGetAsync();
                TempData["ErrorMessage"] = "Please select a mission and assign at least one user.";
                return RedirectToPage();
            }

            foreach (var assignment in Assignments)
            {
                assignment.TenantId = SelectedMissionId;
            }

            if (Assignments.Any(a => a.RoleId <= 0))
            {
                TempData["ErrorMessage"] = "Please assign roles to all users.";
                return RedirectToPage();
            }


            try
            {
                var exists = await _api.PostJsonAsync<
                    List<MissionPreAssignmentDto>,
                    List<string>>(
                        "v1/MissionAccesses/bulk/pre-assign",
                        Assignments);

                if (exists != null && exists.Any())
                {
                    TempData["ErrorMessage"] =
                        $"Some users were skipped because they already exist: {string.Join(", ", exists)}";
                }
                else
                {
                    TempData["SuccessMessage"] =
                        "Users assigned to mission successfully.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk mission assignment failed");
                TempData["ErrorMessage"] =
                    "Failed to assign users to mission.";
            }

            return RedirectToPage();
        }


    }
}
