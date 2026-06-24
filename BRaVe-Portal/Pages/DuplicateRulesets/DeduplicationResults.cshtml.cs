using BRaVe_Portal.Extensions;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static Antlr4.Runtime.Atn.SemanticContext;

namespace BRaVe_Portal.Pages.DuplicateRulesets
{
    public class DeduplicationResultsModel : PageModel
    {

        private readonly ILogger<DeduplicationResultsModel> _logger;

        private readonly IRestApiService _api;

        private readonly IAppCache _cache;

        public DeduplicationResultsModel(ILogger<DeduplicationResultsModel> logger, IAppCache cache, IRestApiService api)
        {
            _logger = logger;
            _cache = cache;
            _api = api;
        }

        public List<DuplicateMatchResult> Results { get; set; }
        public long JobId { get; set; }

        public double? MinScore { get; set; } = 50;
        public double? MaxScore { get; set; } = 1000;

        public static int DURATION_IN_MIN = 5;

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalPages { get; set; }

        public int TotalCount { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }
        public PredicateEvaluationViewModel? ComparisonResult { get; set; }


        public async Task<IActionResult> OnGet(long JobId, double minScore = -1)
        {
            if (Request.ContainsPathProbeInQuery())
            {
                _logger.LogError("Blocked suspicious query pattern on DeduplicationJobResults page.");
                return BadRequest("Invalid request.");
            }

            if (JobId <= 0)
                return RedirectToPage("/DuplicateRulesets/DeduplicationJobs");

            this.JobId = JobId;

            try
            {
                ApiResponse<DuplicateMatchResultsViewModel> response = new ApiResponse<DuplicateMatchResultsViewModel>();


                response = await _api.GetAsync<ApiResponse<DuplicateMatchResultsViewModel>>(
                    $"v1/DeduplicationEngine/dedupresults/{JobId}?pageNumber={PageNumber}&pageSize={PageSize}");


                if (response == null || !response.success)
                {
                    return RedirectToPage("/DuplicateRulesets/DeduplicationJobs");
                }

                // pagination values (must always run)
                TotalCount = response.data.TotalCount;
                TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

                if (minScore < 0)
                    minScore = response.data.MinScore;

                MaxScore = response.data.MaxScore;

                MinScore = Math.Clamp(minScore, 0, MaxScore.Value);

                Results = response.data.Records;

                Results = Results.Where(r => r.TotalScore >= MinScore)
                    .OrderByDescending(r => r.TotalScore)
                    .ToList();

                return Page();

            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error while loading job result.");

                TempData["ErrorMessage"] = "Error while loading job result.";
            }

            return RedirectToPage("/DuplicateRulesets/DeduplicationJobs");


        }


        public async Task<PartialViewResult> OnGetComparePartial(int jobId, string MemberId1, string MemberId2)
        {
            var dto = new IndividualPairDto
            {
                JobId = jobId,
                MemberId1 = MemberId1,
                MemberId2 = MemberId2
            };

            var result = await _api.PostJsonAsync<IndividualPairDto, PredicateEvaluationViewModel>(
                "v1/DuplicateRulesets/identity/compare",
                dto
            );

            return Partial("_ScoreMetricsPartial", result);
        }

    }
}
