using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace BRaVe_Portal.Pages.Preferences
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IAppCache _cache;
        private readonly IRestApiService _api;
        private readonly ILookupService _lookupService;

        public IndexModel(
            ILogger<IndexModel> logger,
            IAppCache cache,
            IRestApiService api,
            ILookupService lookupService)
        {
            _logger = logger;
            _cache = cache;
            _api = api;
            _lookupService = lookupService;
        }

      
        [BindProperty]
        public List<Preference> Preferences { get; set; } = new();

        //[BindProperty(SupportsGet = false)]
        //public List<int>? SelectedPreferenceIds { get; set; }


        //[BindProperty(SupportsGet = false)]
        //public Dictionary<int, string>? PreferenceValues { get; set; }

        [BindProperty]
        public PreferencesDto UpdatedPreferences { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SelectedProgramId { get; set; }

        public List<ProgramDetails> Programs { get; set; } = new();
        public List<LookupItemDto> PreferencesTypes { get; set; } = new();
        public List<MissionPreferences> MissionPreferences { get; set; } = new();

        // ------------------ GET ------------------

        public async Task OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Loading page data...");

                int tenantId = User.Tenant();

                await LoadProgramsAndLookupsAsync();

                MissionPreferences =
                    await _api.GetAsync<List<MissionPreferences>>($"v1/Preferences")
                    ?? new();

                if (!MissionPreferences.Any())
                {
                    Preferences =
                        await _api.GetAsync<List<Preference>>("v1/Preferences/all")
                        ?? new();

                    ViewData["AllPreferences"] = Preferences;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading page data");
            }
            
        }

        private async Task LoadProgramsAndLookupsAsync()
        {
            try
            {
                int tenantId = User.Tenant();

                string programKey = $"{StaticKeyNames.ALL_PROGRAMS_IN_MISSION}{tenantId}";

                if (await _cache.ExistsAsync(programKey))
                {
                    Programs = await _cache.GetAsync<List<ProgramDetails>>(programKey);
                }
                else
                {
                    Programs = await _api.GetAsync<List<ProgramDetails>>("v1/Programs") ?? new();
                    await _cache.SetAsync(programKey, Programs);
                }

                ViewData["Programs"] = Programs;

                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                CoreLookups lookups = await _lookupService.GetCoreLookups(languageCode);

                PreferencesTypes = lookups.PreferenceTypes;
                ViewData["CoreLookups"] = lookups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading programs and lookups");
            }
           
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddMissionPreferencesAsync(Dictionary<int, string> PreferenceValues)
        {

            /*var dtoList = PreferenceValues.Select(kvp => new MissionPreferencesDto
            {
                Id = kvp.Key,
                DefaultValue = kvp.Value,
                ProgramId = UpdatedPreferences.ProgramId,
                TenantId = User.Tenant(),
                CreatedByUserId = User.Identity?.Name ?? "system",
                CreatedOn = DateTime.UtcNow,
                UpdatedByUserId = User.Identity?.Name ?? "system",
                UpdatedOn = DateTime.UtcNow
            }).ToList();*/

            try
            {
                //await _api.PostJsonAsync<List<MissionPreferencesDto>, object>("v1/Preference/create", dtoList);

                await _api.PostJsonAsync< object>("v1/Preferences/create");

                TempData["SuccessMessage"] = "mission preferences attached successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding preferences");
                ModelState.AddModelError(string.Empty, "Failed to add preferences.");
                await OnGetAsync();
                return Page();
            }

            return RedirectToPage(new { SelectedProgramId = UpdatedPreferences.ProgramId });
        }
        // ------------------ UPDATE MULTIPLE PREFERENCES ------------------


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditMissionPreferencesAsync(Dictionary<int, string> PreferenceValues)
        {
            _logger.LogInformation("Processing EditMissionPreferences POST...");

            int tenantId = User.Tenant();
           

            var dtoList = PreferenceValues.Select(pref => new MissionPreferencesDto
            {
                Id = pref.Key,                     // Preference ID
                TenantId = tenantId,
                DefaultValue = pref.Value ?? "",   // Updated default value
                UpdatedByUserId = User.Identity?.Name ?? "system",
                CreatedOn = DateTime.UtcNow,
                CreatedByUserId = User.Identity?.Name ??"system",
                UpdatedOn = DateTime.UtcNow
                
            }).ToList();

            try
            {
                await _api.PostJsonAsync<List<MissionPreferencesDto>, object>(
                    "v1/Preferences/update", dtoList);
                TempData["SuccessMessage"] = "mission preferences updated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating mission preferences.");
                ModelState.AddModelError("", "Error updating mission preferences.");
                await OnGetAsync();
                return Page();
            }

            return RedirectToPage();
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostLoadDefaultsAsync()
        {
            try
            {
                _logger.LogInformation("Loading system default preferences for mission...");

                int tenantId = User.Tenant();

                if (tenantId <= 0)
                {
                    _logger.LogWarning("Invalid TenantId: {TenantId}", tenantId);
                    TempData["ErrorMessage"] = "Invalid tenant context.";
                    return Page();
                }

                // -----------------------------
                // Load data (API calls)
                // -----------------------------
                var missionTask = _api.GetAsync<List<MissionPreferences>>($"v1/Preferences/{tenantId}");
                var defaultTask = _api.GetAsync<List<Preference>>("v1/Preferences/all");

                await Task.WhenAll(missionTask, defaultTask);

                var missionPrefs = missionTask.Result ?? new List<MissionPreferences>();
                var defaultPrefs = defaultTask.Result ?? new List<Preference>();

                // -----------------------------
                // SINGLE-PASS LOOKUP SET
                // -----------------------------
                var missionLookup = missionPrefs.ToDictionary(x => x.PreferenceId);

                // -----------------------------
                // SINGLE-PASS TRANSFORM (NO NESTED LOOPS)
                // -----------------------------
                var mergedPrefs = defaultPrefs.Select(dp =>
                {
                    missionLookup.TryGetValue(dp.PreferenceId, out var existing);

                    return new MissionPreferences
                    {
                        PreferenceId = dp.PreferenceId,
                        PreferenceName = dp.PreferenceName,
                        Description = dp.Description,
                        PreferenceType = dp.PreferenceType,
                        DefaultValue = existing?.DefaultValue ?? dp.DefaultValue,
                        IsFromDefault = existing == null
                    };
                }).ToList();

                // -----------------------------
                // RESULT ASSIGNMENT (NO MUTATION LOOPS)
                // -----------------------------
                MissionPreferences = mergedPrefs;

                Preferences = defaultPrefs
                    .Select(p => new Preference
                    {
                        PreferenceId = p.PreferenceId,
                        PreferenceName = p.PreferenceName,
                        Description = p.Description,
                        PreferenceType = p.PreferenceType,
                        DefaultValue = p.DefaultValue,
                        IsFromDefault = true
                    })
                    .ToList();

                ViewData["AllPreferences"] = Preferences;

                await LoadProgramsAndLookupsAsync();

                TempData["SuccessMessage"] =
                    "Default preferences loaded and merged successfully.";

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while loading default preferences");

                TempData["ErrorMessage"] =
                    $"An unexpected error occurred: {ex.Message}";

                return Page();
            }
        }


    }
}
