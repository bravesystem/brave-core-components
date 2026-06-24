using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace BRaVe_Portal.Pages.Monitoring
{
    public class BiometricMatchComparisonModel : PageModel
    {
        private readonly IRestApiService _api;
        private readonly ILogger<BiometricMatchComparisonModel> _logger;

        public BiometricMatchComparisonModel(
            IRestApiService api,
            ILogger<BiometricMatchComparisonModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalPages { get; set; }

        public int TotalCount { get; set; }

        public int StartRecord =>
            TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;

        public int EndRecord =>
            Math.Min(PageNumber * PageSize, TotalCount);

        [BindProperty(SupportsGet = true)]
        public string? ActivityCode { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateTo { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? MinScore { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public List<BiometricMatchListDto> Matches { get; set; } = new();

        public List<string> ActivityCodes { get; set; } = new();
        public List<AdjudicationDecisionDto> AdjudicationDecisions { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                var decisions =await _api.GetAsync<ApiResponseDto<List<AdjudicationDecisionDto>>>("v1/adjudication/decisions");

                AdjudicationDecisions = decisions?.Data ?? new();
                var response =
                    await _api.GetAsync<PagedResult<BiometricMatchListDto>>(
                        $"v1/biometricMatches/list?" +
                        $"pageNumber={PageNumber}" +
                        $"&pageSize={PageSize}" +
                        $"&activity={ActivityCode}" +
                        $"&dateFrom={(DateFrom.HasValue ? DateFrom.Value.ToString("yyyy-MM-dd") : "")}" +
                        $"&dateTo={(DateTo.HasValue ? DateTo.Value.ToString("yyyy-MM-dd") : "")}" +
                        $"&minScore={MinScore}",
                        HttpContext.RequestAborted);

                if (response != null)
                {
                    Matches = response.Items;
                    TotalCount = response.TotalCount;

                    TotalPages =
                        (int)Math.Ceiling(response.TotalCount / (double)PageSize);
                }

                var activities =
                    await _api.GetAsync<List<string>>(
                        "v1/biometricMatches/activities",
                        HttpContext.RequestAborted);

                if (activities != null)
                {
                    ActivityCodes = activities;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed loading biometric matches");
            }
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAdjudicateBulkAsync([FromBody] List<AdjudicationRequestDto> requests)
        {
            try
            {
                await _api.PostJsonAsync<object, ApiResponseDto<object>>(
                    "v1/adjudication/adjudicate-bulk",
                    requests);

                TempData["SuccessMessage"] =
                    $"{requests.Count} records adjudicated successfully.";

                return new JsonResult(new
                {
                    redirect = Url.Page("/Monitoring/BiometricMatchComparison")
                });
            }
            catch
            {
                return new JsonResult(new
                {
                    redirect = Url.Page("/Monitoring/BiometricMatchComparison")
                });
            }
        }
    }
}