using BRaVe_Portal.Extensions;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text.Json;

namespace BRaVe_Portal.Pages.Targeting
{
    public class ApplyTargettingModel : PageModel
    {
        private readonly IRestApiService _api;
        private readonly ILogger<ApplyTargettingModel> _logger;

        public ApplyTargettingModel(
            IRestApiService api,
            ILogger<ApplyTargettingModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        [BindProperty]

        public int TargetingId { get; set; }

        [BindProperty]
        public DateTime FromDate { get; set; }

        [BindProperty]
        public DateTime ToDate { get; set; }


        public int? SelectedDistributionId { get; set; }

        public List<TargetingRuleDto> TargetingRules { get; private set; } = new();

        public int? TargetingRuleId { get; set; }

        public List<TargetingJobDto> Jobs { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        public class ApiErrorResponse
        {
            public bool success { get; set; }
            public string errorMessage { get; set; }
        }

        public async Task OnGetAsync(int? targetingId, DateTime? fromDate, DateTime? toDate)
        {
            FromDate = fromDate ?? DateTime.Today.AddDays(-30);
            ToDate = toDate ?? DateTime.Today;

            if (targetingId.HasValue)
                TargetingId = targetingId.Value;

            // Get targeting rules
            try
            {
                TargetingRules = await _api.GetAsync<List<TargetingRuleDto>>("v1/TargetingRules")
                            ?? new List<TargetingRuleDto>();

                TargetingRules = TargetingRules
                    .Where(d => d.IsValid == true && d.IsCustom == true)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching TargetingRules.");
                TargetingRules = new();
            }

            //Get jobs

            try
            {
                var result = await _api.GetAsync<PagedTargetingJobsDto>(
                    $"v1/SearchEngine/targetingjobs?pageNumber={PageNumber}&pageSize={PageSize}");

                if (result != null && result.Success)
                {
                    Jobs = result.Data;
                    TotalPages = result.TotalPages;
                    TotalRecords = result.TotalRecords;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading targeting jobs.");
            }


        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostApplyAsync()
        {
            try
            {
                var payload = new
                {
                    targetingId = TargetingId,
                    range = new
                    {
                        from_date = FromDate.ToString("yyyy-MM-dd"),
                        to_date = ToDate.ToString("yyyy-MM-dd")
                    }
                };

                await _api.PostJsonAsync<object, object>(
                    "v1/SearchEngine/targetingjob",
                    payload
                );

                TempData["SuccessMessage"] = "Targeting job created successfully.";
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.LogError(ex, "Error creating targeting job.");

                var apiError = JsonSerializer.Deserialize<ApiErrorResponse>(
                           ex.Message);

                TempData["ErrorMessage"] = apiError.errorMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating targeting job.");

                TempData["ErrorMessage"] = "Error creating targeting job.";


            }

            return RedirectToPage(new { pageNumber = PageNumber, pageSize = PageSize });
        }


        public async Task<IActionResult> OnPostClearJobAsync(long jobId)
        {
            try
            {
                await _api.PostJsonAsync<object>(
                    $"v1/SearchEngine/disablejob?jobId={jobId}");

                TempData["SuccessMessage"] = "Job disabled successfully.";
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.LogError(ex, "Error disabling job.");

                var apiError = JsonSerializer.Deserialize<ApiErrorResponse>(
                           ex.Message);

                TempData["ErrorMessage"] = apiError.errorMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling job.");

                string errorMessage = "An error occurred while disabling the job.";

                TempData["ErrorMessage"] = errorMessage;
            }

            return RedirectToPage(new { pageNumber = PageNumber, pageSize = PageSize });
        }








    }
}
