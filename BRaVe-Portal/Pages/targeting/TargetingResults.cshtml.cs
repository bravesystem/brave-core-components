using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.Targeting
{
    public class TargetingResultsModel : PageModel
    {
        private readonly IRestApiService _api;
        private readonly ILogger<TargetingResultsModel>
    _logger;

        public TargetingResultsModel(
        IRestApiService api,
        ILogger<TargetingResultsModel>
            logger)
        {
            _api = api;
            _logger = logger;
        }

        // Query string parameters
        [BindProperty(SupportsGet = true)]
        public int TargetingId { get; set; }


        [BindProperty(SupportsGet = true)]
        public string TargetingRule { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime ToDate { get; set; }


        public List<TargetingResultDto> Results { get; set; } = new();

        public List<DistributionTypes> Distributions { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public long JobId { get; set; }


        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public T Data { get; set; }
        }


        public async Task OnGetAsync()
        {
            if (JobId == 0)
                return;

            await LoadResultsAsync();

        }

        private async Task LoadResultsAsync()
        {
            // 1 Load Distributions (unchanged behavior)
            try
            {
                Distributions = await _api.GetAsync<List<DistributionTypes>>("v1/DistributionsType")
                                ?? new List<DistributionTypes>();

                Distributions = Distributions
                    .Where(d => d.IsActive.HasValue && d.IsActive.Value)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading distributions.");
                Distributions = new List<DistributionTypes>();
            }

            // 2 Load Targeting Results by JobId
            try
            {
                //var response = await _api.GetAsync<ApiResponse<List<TargetingResultDto>>>(
                //    $"v1/SearchEngine/targetingresults?jobId={JobId}"
                //);

                var response = await _api.GetAsync<ApiResponse<List<TargetingResultDto>>>(
                    $"v1/SearchEngine/targetingresults/{JobId}"
                );

                if (response != null && response.Success && response.Data != null)
                {
                    Results = response.Data;
                }
                else
                {
                    Results = new List<TargetingResultDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading targeting results.");
                Results = new List<TargetingResultDto>();
            }
        }



        [ValidateAntiForgeryToken]

        public async Task<IActionResult> OnPostEnrollBeneficiariesAsync([FromBody] EnrollBeneficiariesDto dto)
        {
            try
            {
                //dto.TargetingId
                //    = TargetingId;

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
    }
}
