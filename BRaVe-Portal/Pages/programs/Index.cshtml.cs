using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Models.Enums;
using BRaVe_Portal.Models.ViewModels;
using BRaVe_Portal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace BRaVe_Portal.Pages.programs
{
    [Authorize]
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

        public List<LookupItemDto> CommonStatuses { get; set; } = new();
        public List<ProgramDto> Programs { get; set; } = new();

        [BindProperty]
        public ProgramDto UpdateProgram { get; set; } = new();

        // assessment lists 
        [BindProperty]
        public List<AssessmentViewModel> Ongoing_Assessments { get; set; } = new();

        [BindProperty]
        public List<AssessmentViewModel> Decided_Assessments { get; set; } = new();

        public List<SystemUserDto> SystemUsers { get; set; } = new();


        public async Task OnGetAsync()
        {
            _logger.LogInformation("OnGetAsync called for Programs IndexModel");

            int TenantId = User.Tenant();
            _logger.LogInformation("TenantId: {TenantId}", TenantId);

            // load assessments so Razor can find assessment info per program
            List<AssessmentViewModel> assessments = new();
            try
            {
                assessments = await _api.GetAsync<List<AssessmentViewModel>>(
                    $"v1/RiskBenefitAssessments/all/{CultureInfo.CurrentCulture.TwoLetterISOLanguageName}"
                ) ?? new List<AssessmentViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not load assessments for Programs page");
            }

            Ongoing_Assessments = assessments;/*==null? new List<AssessmentViewModel>():assessments
                .Where(a => a.Status != AssessmentStatus.Approved && a.Status != AssessmentStatus.Rejected)
                .ToList();*/

            /*Decided_Assessments = assessments == null ? new List<AssessmentViewModel>() : assessments
                .Where(a => a.Status == AssessmentStatus.Approved || a.Status == AssessmentStatus.Rejected)
                .ToList();*/


            try
            {
                Programs = await _api.GetAsync<List<ProgramDto>>("v1/Programs")
                    ?? new List<ProgramDto>();

                await LoadProgramStatusesAsync();

            // after loading programs & statuses, load system users for selects
            await LoadSystemUsersAsync();

            _logger.LogInformation("Fetched {Count} programs from API", Programs.Count);

                await _cache.SetAsync($"{StaticKeyNames.ALL_PROGRAMS_IN_MISSION}{TenantId}", Programs);
                _logger.LogInformation("Programs cached for TenantId {TenantId}", TenantId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error fetching programs from API");
                throw;
            }
            
        }

        //CREATE NEW PROGRAM

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostCreateProgram(ProgramDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Page();

                // Read selected users CSV from the form (hidden input populated by JS)
                var selectedCsv = Request.Form["SelectedRoleIdsHidden"].FirstOrDefault() ?? string.Empty;
                var tenantId = User.Tenant();

                if (!string.IsNullOrWhiteSpace(selectedCsv))
                {
                    var selectedUserIds = selectedCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                                     .Where(s => !string.IsNullOrWhiteSpace(s))
                                                     .Distinct(StringComparer.OrdinalIgnoreCase)
                                                     .ToList();


                    var assignments = new List<UserProgAssignmentDto>();
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


                    // Ensure SystemUsers populated (fallback to loading if empty)
                    if (SystemUsers == null || !SystemUsers.Any())
                    {
                        await LoadSystemUsersAsync();
                    }

                    foreach (var userId in selectedUserIds)
                    {
                        var user = SystemUsers.FirstOrDefault(u => string.Equals(u.UserId, userId, StringComparison.OrdinalIgnoreCase));
                        if (user == null) continue;

                        // user.RoleIds may be string/int list depending on your SystemUserDto.
                        // Try RoleIds (List<int>), then RoleIdsString (List<string>), then Roles (List<string>) fallback.
                        List<int> roleIds = new();

                        // try common properties using reflection-safe checks
                        var type = user.GetType();
                        var propIntList = type.GetProperty("RoleIds");
                        if (propIntList != null)
                        {
                            var val = propIntList.GetValue(user);
                            if (val is IEnumerable<int> ints)
                                roleIds.AddRange(ints);
                            else if (val is IEnumerable<string> strs)
                                roleIds.AddRange(strs.Select(s => int.TryParse(s, out var x) ? x : -1).Where(x => x > 0));
                        }
                        // fallback to Roles (names) - skip since we need numeric RoleId
                        // If no numeric role IDs found, skip user
                        if (!roleIds.Any()) continue;

                        foreach (var rid in roleIds.Distinct())
                        {
                            var key = $"{userId}|{rid}|{tenantId}";
                            if (seen.Add(key))
                            {
                                assignments.Add(new UserProgAssignmentDto
                                {
                                    UserId = userId,
                                    RoleId = rid,
                                    TenantId = tenantId
                                });
                            }
                        }
                    }

                    if (assignments.Any())
                    {
                        dto.UserProgAssignments = assignments;
                    }
                }

                await _api.PostJsonAsync<ProgramDto, object>("v1/Programs/create", dto);

                TempData["SuccessMessage"] = "Program created successfully.";

                await _cache.RemoveAsync($"{StaticKeyNames.ALL_PROGRAMS_IN_MISSION}{User.Tenant()}");

                return RedirectToPage("/Programs/Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Unable to create Program from the selected Assessment—please try again");
                _logger.LogError(ex, "Error creating Program for Assessment={AssessmentId}", dto.AssessmentId);
                return RedirectToPage("Index");
            }
        }




        //CREATE RISK BENEFIT ASSESSMENT FROM PROGRAM ID

        public async Task<IActionResult> OnPost(int programId, bool requiresApproval)
        {

            try
            {
                DataProcessingDto dataProcessingDto = new DataProcessingDto()
                {
                    ProgramId = programId,
                    RequireApprovals = requiresApproval,
                    ProgamManagerName = User.DisplayName(),
                    ProgamManagerContacts = User.GetUserEmailLike()
                };

                RiskBenefitAssessmentDto riskBenefitAssessmentDto = await _api.PostJsonAsync<DataProcessingDto, RiskBenefitAssessmentDto>($"v1/RiskBenefitAssessments/create", dataProcessingDto);
                int tenant = User.Tenant();

                TempData.Put("NewAssessment", riskBenefitAssessmentDto);

                return RedirectToPage("/Assessments/EditAssessment", new { assessmentId = riskBenefitAssessmentDto.AssessmentId, statusId = 1 });

            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Could not create Assessment: {ex.Message}");

            }

            return RedirectToPage("");

        }

        private async Task LoadProgramStatusesAsync()
        {
            // Load program status from Lookups
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);
            CommonStatuses = coreLookups.CommonStatuses;
            ViewData["CoreLookups"] = coreLookups;
            _logger.LogInformation("ProgramStatus loaded. Count: {Count}", CommonStatuses.Count);
        }


        //EDIT PROGRAM 
        public async Task<IActionResult> OnPostEditProgram()
        {
            _logger.LogInformation("Edit Program: ProgramId {ProgramId}", UpdateProgram.ProgramId);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalid when editing program {ProgramId}.", UpdateProgram.ProgramId);
                await OnGetAsync();
                return Page();
            }

            try
            {
                // Read selected users CSV from the form (hidden input populated by JS)
                var selectedCsv = Request.Form["SelectedRoleIds"].FirstOrDefault() ?? string.Empty;
                var tenantId = User.Tenant();

                if (!string.IsNullOrWhiteSpace(selectedCsv))
                {
                    var selectedUserIds = selectedCsv
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    // Ensure SystemUsers are loaded
                    if (SystemUsers == null || !SystemUsers.Any())
                        await LoadSystemUsersAsync();

                    var assignments = new List<UserProgAssignmentDto>();
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var userId in selectedUserIds)
                    {
                        var user = SystemUsers.FirstOrDefault(u => string.Equals(u.UserId, userId, StringComparison.OrdinalIgnoreCase));
                        if (user == null) continue;

                        // Expect SystemUserDto to expose RoleIds as List<int>
                        var roleIds = new List<int>();
                        if (user.RoleIds != null && user.RoleIds.Any())
                        {
                            roleIds.AddRange(user.RoleIds);
                        }
                        else if (user.Roles != null && user.Roles.Any())
                        {
                            // fallback: try parse role strings if RoleIds not present
                            roleIds.AddRange(user.Roles
                                .Select(r => int.TryParse(r, out var x) ? x : -1)
                                .Where(x => x > 0));
                        }

                        if (!roleIds.Any()) continue;

                        foreach (var rid in roleIds.Distinct())
                        {
                            var key = $"{userId}|{rid}|{tenantId}";
                            if (seen.Add(key))
                            {
                                assignments.Add(new UserProgAssignmentDto
                                {
                                    UserId = userId,
                                    RoleId = rid,
                                    TenantId = tenantId
                                });
                            }
                        }
                    }

                    if (assignments.Any())
                        UpdateProgram.UserProgAssignments = assignments;
                }

                await _api.PostJsonAsync<ProgramDto, object>("v1/Programs/update", UpdateProgram);
                _logger.LogInformation("Program {ProgramId} updated successfully.", UpdateProgram.ProgramId);

                TempData["SuccessMessage"] = "Program updated successfully.";
                // Clear tenant cache after update
                int tenant = User.Tenant();
                await _cache.RemoveAsync($"{StaticKeyNames.ALL_PROGRAMS_IN_MISSION}{tenant}");
                return RedirectToPage("/Programs/Index");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error updating program {ProgramId}", UpdateProgram.ProgramId);
                ModelState.AddModelError(string.Empty, "Could not update program. Please contact the program manager.");
                await OnGetAsync();
                return Page();
            }

           
        }



        /// Loads system users from the AccessControl API and stores them on the PageModel and ViewData.

        private async Task LoadSystemUsersAsync(CancellationToken ct = default)
        {
            try
            {
                var userId = User.Identifier();

                // Call the AccessControl endpoint you created
                var users = await _api.GetAsync<List<SystemUserDto>>($"v1/AccessControl/system-users", ct)
                           ?? new List<SystemUserDto>();

                // Exclude logged-in user
                SystemUsers = users
                     .Where(u => !string.Equals(u.UserId, userId, StringComparison.OrdinalIgnoreCase))
                     .ToList();

                // Make available to partials Razor via ViewData (same approach as CoreLookups)
                ViewData["SystemUsers"] = SystemUsers;

                _logger.LogInformation("Loaded {Count} system users for tenant", SystemUsers.Count);
            }
            catch (Exception ex)
            {
                // don't fail the whole page when users can't be loaded; just log and continue
                _logger.LogWarning(ex, "Could not load system users for populating program user selects.");
                SystemUsers = new List<SystemUserDto>();
                ViewData["SystemUsers"] = SystemUsers;
            }
        }

    }
}
