using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Models.Enums;
using BRaVe_Portal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace BRaVe_Portal.Pages.Dashboard
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        //private readonly IAppCache _cache;
        private readonly IRestApiService _api;

        public IndexModel(
            ILogger<IndexModel> logger,
            /*IAppCache cache,*/
            IRestApiService api)
        {
            _logger = logger;
            //f_cache = cache;
            _api = api;
        }

        // Filters / lookups
        public List<MissionDto> Missions { get; set; } = new();
        public List<ProgramDetails> Programs { get; set; } = new();
        public List<ActivityViewModel> Activities { get; set; } = new();

        // DASHBOARD DATA (Head-of-household only)
        public List<DashboardRowDto> RawDashboardData { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SelectedProgramId { get; set; }


        [BindProperty(SupportsGet = true)]
        public int? SelectedActivityId { get; set; }


        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }


        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }


        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }


        [BindProperty(SupportsGet = true)]
        public List<DistributionTypes> Distributions { get; set; }

        public List<AssessmentViewModel> Ongoing_Assessments { get; set; } = new();

        public List<AssessmentViewModel> Decided_Assessments { get; set; } = new();

        [BindProperty]
        public string FlaggedBeneficiariesJson { get; set; }

        [BindProperty]
        public string FlagReason { get; set; }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEnrollBeneficiariesAsync( [FromBody] EnrollBeneficiariesDto dto)
        {
            _logger.LogInformation(
                "Portal enroll request received. DistributionId={DistributionId}, HouseholdCount={HouseholdCount}",
                dto.DistributionId,
                dto.HouseholdIds?.Count ?? 0
            );


            try
            {
               

                await _api.PostJsonAsync<EnrollBeneficiariesDto, object>(
                    "v1/TargetingRules/enroll-beneficiaries",
                    dto
                );

                return new JsonResult(new
                {
                    success = true,
                    redirectUrl = Url.Page("/assistances/Index",
                        new { SelectedDistributionId = dto.DistributionId })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enrollment failed.");

                return new JsonResult(new
                {
                    success = false,
                    message = "Enrollment failed."
                });
            }

        }


        public class EnrollRequest
        {
            public List<string> Selected { get; set; } = new();
            public int Distribution { get; set; }
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostFlagBeneficiariesAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FlaggedBeneficiariesJson))
                {
                    TempData["ErrorMessage"] =
                        "No beneficiaries selected.";

                    return RedirectToPage();
                }

                var beneficiaries =
    JsonSerializer.Deserialize<List<FlagBeneficiaryRequest>>(
        FlaggedBeneficiariesJson,
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

                if (beneficiaries == null || !beneficiaries.Any())
                {
                    TempData["ErrorMessage"] =
                        "No beneficiaries selected.";

                    return RedirectToPage();
                }

                await _api.PostJsonAsync<object, object>("v1/FlaggedBeneficiaries",
                    new
                    {
                        beneficiaries,
                        reason = FlagReason
                    });

                TempData["SuccessMessage"] =
                    "Beneficiaries flagged successfully.";

                return Redirect("/Flagged/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flagging beneficiaries");

                TempData["ErrorMessage"] =
    $"Failed to flag beneficiaries. {ex.Message}";

                return RedirectToPage();
            }
        }

        public async Task<JsonResult> OnGetGraphDataAsync(int? tenantId, int? programId, DateTime? startDate, DateTime? endDate)
        {


            if (Request.Query.Count > 0 &&
               Request.Query.Any(q => StringHelper.IsPotentialPathProbe(q.Value.ToString())))
            {
                _logger.LogWarning("Blocked suspicious query payload on GraphData GET.");
                TempData["ErrorMessage"] = "Invalid request.";
                new JsonResult(new DashboardGraphDto());
            }



            // fallback defaults
            var from = startDate ?? DateTime.Today.AddMonths(-1);
            var to = endDate ?? DateTime.Today;

            try
            {
                // Call the new backend endpoint
                var url = $"v1/Dashboard/graphdata?tenantId={tenantId}&programId={programId}&startDate={from:yyyy-MM-dd}&endDate={to:yyyy-MM-dd}";
                var result = await _api.GetAsync<DashboardGraphDto>(url);

                return new JsonResult(result ?? new DashboardGraphDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed loading dashboard graph data");
                return new JsonResult(new DashboardGraphDto());
            }
        }
        
        public async Task<IActionResult> OnGetAsync()
        {
            _logger.LogInformation("Dashboard loading");

            if (Request.Query.Count > 0 &&
              Request.Query.Any(q => StringHelper.IsPotentialPathProbe(q.Value.ToString())))
            {
                _logger.LogWarning("Blocked suspicious query payload on MangeActivities GET.");
                TempData["ErrorMessage"] = "Invalid request.";

                return RedirectToPage();
            }

            

            // Load dashboard data (OPTIMISED ENDPOINT)
            try
            {

                // Programs
                Programs = await _api.GetAsync<List<ProgramDetails>>("v1/Programs") ?? new();

                Distributions = await _api.GetAsync<List<DistributionTypes>>("v1/DistributionsType") ?? new List<DistributionTypes>();

                Distributions = Distributions
                    .Where(d => d.IsActive == true)
                    .ToList();

                var assessments = await _api.GetAsync<List<AssessmentViewModel>>(
                        $"v1/RiskBenefitAssessments/all/en"
                    ) ?? new List<AssessmentViewModel>();

                Ongoing_Assessments = assessments
                    .Where(a => a.Status != AssessmentStatus.Approved && a.Status != AssessmentStatus.Rejected)
                    .ToList();

                Decided_Assessments = assessments
                    .Where(a => a.Status == AssessmentStatus.Approved || a.Status == AssessmentStatus.Rejected)
                    .ToList();

                // Missions (role-aware)
                bool isAdmin = User.IsInRole("Admin");

                if (isAdmin)
                {
                    Missions = await LoadAllMissionsAsync();
                }
                else
                {
                    var missionNames = User.Mission()?
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim())
                        .ToList() ?? new();

                    var allMissions = await LoadAllMissionsAsync();
                    Missions = allMissions
                        .Where(m => missionNames.Contains(m.Name, StringComparer.OrdinalIgnoreCase))
                        .ToList();
                }


                 var url =
                     $"v1/dashboard" +
                     $"?programId={SelectedProgramId}" +
                     $"&activityId={SelectedActivityId}" +
                     $"&startDate={StartDate:yyyy-MM-dd}" +
                     $"&endDate={EndDate:yyyy-MM-dd}";

                RawDashboardData =
                    await _api.GetAsync<List<DashboardRowDto>>(url)
                    ?? new();

                _logger.LogInformation("Succeeded loading dashboard data");

                // Server-side search (still in-memory, but now small dataset)
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var term = SearchTerm.ToLower();

                    RawDashboardData = RawDashboardData
                        .Where(r =>
                            (r.HouseholdId ?? "").ToLower().Contains(term) ||
                            (r.HeadFullName ?? "").ToLower().Contains(term) ||
                            (r.MissionName ?? "").ToLower().Contains(term) ||
                            (r.ActivityTitle ?? "").ToLower().Contains(term))
                        .ToList();
                }
                 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed loading dashboard data");
                RawDashboardData = new();
                TempData["ErrorMessage"] = "Failed loading dashboard data.";
            }
            return Page();
        }

        private async Task<List<MissionDto>> LoadAllMissionsAsync()
        {
            var missions = await _api.GetAsync<List<MissionDto>>("v1/Missions") ?? new();
            return missions;
        }


        /// <summary>
        /// Returns programs filtered by mission for the Program dropdown.
        /// Call with missionId= for a specific mission, or omit for all programs.
        /// </summary>
        public async Task<JsonResult> OnGetProgramsByMissionAsync(int? missionId)
        {
            try
            {

                int _missionId = missionId.HasValue ? missionId.Value : -1;
                // Build URL dynamically
                var url = $"v1/Programs/{_missionId}";

                // Use the URL we built
                var programs = await _api.GetAsync<List<ProgramDetails>>(url) ?? new List<ProgramDetails>();
                return new JsonResult(programs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load programs by mission");
                return new JsonResult(new List<ProgramDetails>());
            }
        }

        //Gets activities
        public async Task<JsonResult> OnGetActivitiesByProgramAsync(int? programId)
        {
            try
            {
                if (programId == null)
                {
                    return new JsonResult(new List<ActivityViewModel>());
                }

                var activities =
                    await _api.GetAsync<List<ActivityViewModel>>(
                        $"v1/Activities/program/{programId}")
                    ?? new List<ActivityViewModel>();

                return new JsonResult(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed loading activities");

                return new JsonResult(new List<ActivityViewModel>());
            }
        }

        // AJAX reload for raw dashboard table

        //[ValidateAntiForgeryToken]
        public async Task<JsonResult> OnGetRawDataAsync()
        {
            try
            {
                var data = await _api.GetAsync<List<DashboardRowDto>>(
                    "v1/dashboard")
                    ?? new();

                /*Distributions = await _api.GetAsync<List<Distribution>>("v1/Distributions")
                    ?? new List<Distribution>();*/

                return new JsonResult(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load raw dashboard data in handler");
                return new JsonResult(new List<DashboardRowDto>());
            }
        }
    }
}
