using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.Monitoring
{
    public class AdjudicationHistoryModel : PageModel
    {
        private readonly IRestApiService _api;
        private readonly ILogger<AdjudicationHistoryModel> _logger;

        public AdjudicationHistoryModel(
            IRestApiService api,
            ILogger<AdjudicationHistoryModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        //---------------------------------------
        // PAGINATION
        //---------------------------------------

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalPages { get; set; }

        public int TotalCount { get; set; }

        public int StartRecord =>
            TotalCount == 0
                ? 0
                : ((PageNumber - 1) * PageSize) + 1;

        public int EndRecord =>
            Math.Min(PageNumber * PageSize, TotalCount);



        //---------------------------------------
        // FILTERS
        //---------------------------------------

        [BindProperty(SupportsGet = true)]
        public int? DecisionId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? DedupMode { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateTo { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? AdjudicatedBy { get; set; }



        //---------------------------------------
        // DATA
        //---------------------------------------

        public List<AdjudicationHistoryDto> Records { get; set; } = new();

        public List<AdjudicationDecisionDto> AdjudicationDecisions { get; set; } = new();



        //---------------------------------------
        // ALERTS
        //---------------------------------------

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }



        //---------------------------------------
        // LOAD PAGE
        //---------------------------------------

        public async Task OnGetAsync()
        {
            try
            {
                //---------------------------------------
                // LOAD DECISIONS
                //---------------------------------------

                var decisions =
                    await _api.GetAsync<ApiResponseDto<List<AdjudicationDecisionDto>>>(
                        "v1/adjudication/decisions");

                AdjudicationDecisions =
                    decisions?.Data ?? new();



                //---------------------------------------
                // LOAD HISTORY
                //---------------------------------------

                var response =
   await _api.GetAsync<ApiResponseDto<PagedResult<AdjudicationHistoryDto>>>(
                        $"v1/adjudication/history?" +
                        $"pageNumber={PageNumber}" +
                        $"&pageSize={PageSize}" +
                        $"&decisionId={DecisionId}" +
                        $"&dedupMode={DedupMode}" +
                        $"&dateFrom={(DateFrom.HasValue ? DateFrom.Value.ToString("yyyy-MM-dd") : "")}" +
                        $"&dateTo={(DateTo.HasValue ? DateTo.Value.ToString("yyyy-MM-dd") : "")}" +
                        $"&adjudicatedBy={AdjudicatedBy}",
                        HttpContext.RequestAborted);

                if (response?.Data != null)
                {
                    Records = response.Data.Items;

                    TotalCount = response.Data.TotalCount;

                    TotalPages =
                        (int)Math.Ceiling(
                            response.Data.TotalCount / (double)PageSize);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed loading adjudication history");
                ErrorMessage =
      $"Failed loading adjudication history: {ex.Message}";
            }
        }
    }
}