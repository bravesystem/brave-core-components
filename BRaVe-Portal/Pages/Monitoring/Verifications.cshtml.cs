using Azure;
using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.Verifications
{
    public class VerificationsModel : PageModel
    {
        private readonly ILogger<VerificationsModel> _logger;
        private readonly IRestApiService _api;

        public VerificationsModel(
            ILogger<VerificationsModel> logger,
            IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public int VerificationPageNumber { get; set; } = 1;

        public int VerificationPageSize { get; } = 10;

        public int VerificationTotalPages { get; private set; }

        public bool VerificationLoadFailed { get; private set; }

        public List<BiometricVerificationResultDto> Verifications { get; private set; }
            = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (Request.ContainsPathProbeInQuery())
            {
                _logger.LogError("Blocked suspicious query pattern on Verifications page.");
                return BadRequest("Invalid request.");
            }

            try
            {
                var response =
                    await _api.GetAsync<PagedResult<BiometricVerificationResultDto>>(
                        $"v1/biometricMatches/verifications" +
                        $"?pageNumber={VerificationPageNumber}" +
                        $"&pageSize={VerificationPageSize}"
                    );

                if (response == null)
                {
                    VerificationLoadFailed = true;
                    return Page();
                }

                Verifications = response.Items;

                VerificationTotalPages =
                    (int)Math.Ceiling(
                        response.TotalCount / (double)VerificationPageSize);

                if (VerificationPageNumber < 1)
                    VerificationPageNumber = 1;

                if (VerificationTotalPages > 0 &&
                    VerificationPageNumber > VerificationTotalPages)
                    VerificationPageNumber = VerificationTotalPages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load biometric verifications");
                VerificationLoadFailed = true;
                Verifications = new();
            }

            return Page();
        }
    }
}
