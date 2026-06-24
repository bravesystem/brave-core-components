using BRaVe_Portal.Extensions;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Models.ViewModels;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net;
using System.Text.Json;
using static BRaVe_Portal.Pages.Targeting.ApplyTargettingModel;

namespace BRaVe_Portal.Pages.DuplicateRulesets
{
    public class DeduplicationJobsModel : PageModel
    {

        private readonly ILogger<DeduplicationJobsModel> _logger;

        private readonly IRestApiService _api;

        public DeduplicationJobsModel(ILogger<DeduplicationJobsModel> logger, IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        public List<DeduplicationJobDto> Jobs { get; set; } = new();

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostClearJobAsync(long jobId)
        {

            if (Request.ContainsPathProbeInQuery() ||
               await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query pattern on ClearDeduplicationJob POST.");
                return BadRequest("Invalid request.");
            }

            try
            {
                await _api.PostJsonAsync<object>(
                   $"v1/DeduplicationEngine/disablejob/{jobId}");

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
                _logger.LogError(ex, "Error clearing deduplication job.");

                TempData["ErrorMessage"] = "Error clearing deduplication job.";

            }


            return await OnGet();
        }

        /// <summary>
        /// Handler for Add button: redirects to the Add job page so user can pick ruleset and period.
        /// </summary>
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAdd(int ruleId, DateTime startPeriod, DateTime endPeriod)
        {

            if (Request.ContainsPathProbeInQuery() ||
               await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query pattern on AddDeduplicationJob POST.");
                return BadRequest("Invalid request.");
            }


            try
            {
                await _api.PostJsonAsync<DateRangeDto, object>(
                                    $"v1/DeduplicationEngine/dedupjobs/{ruleId}",  new DateRangeDto { 
                                        from_date = startPeriod.ToString("yyyy-MM-dd"),
                                        to_date = endPeriod.ToString("yyyy-MM-dd")
                                    });

                TempData["SuccessMessage"] = "Deduplication job created successfully.";

            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.LogError(ex, "Error creating deduplication job.");

                var apiError = JsonSerializer.Deserialize<ApiErrorResponse>(
                           ex.Message);

                TempData["ErrorMessage"] = apiError.errorMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating deduplication job.");

                TempData["ErrorMessage"] = "Error creating deduplication job.";

            }


            return await OnGet();
        }

        [BindProperty]
        public DateTime FromDate { get; set; }

        [BindProperty]
        public DateTime ToDate { get; set; }

        [BindProperty]
        public int RuleId { get; set; }
        public List<SelectListItem> RuleItems { get; private set; } = new();
        public async Task<IActionResult> OnGet()
        {

            if (Request.ContainsPathProbeInQuery())
            {
                _logger.LogError("Blocked suspicious query pattern on DeduplicationJob page.");
                return BadRequest("Invalid request.");
            }

            // Get targeting rules
            FromDate = DateTime.Today.AddDays(-30);
            ToDate = DateTime.Today;

            try
            {
                var DuplicateRulesets = await _api.GetAsync<List<DuplicateRulesetViewModel>>("v1/DuplicateRulesets")
                    ?? new List<DuplicateRulesetViewModel>();

                RuleItems = DuplicateRulesets.Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Name,
                }).ToList();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching Deduplication Rules.");
                RuleItems = new();
            }

            try
            {
                var result = await _api.GetAsync<PagedDeduplicationJobsDto>(
                    $"v1/DeduplicationEngine/dedupjobs?pageNumber={PageNumber}&pageSize={PageSize}");

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

            return Page();
        }
    }
}
