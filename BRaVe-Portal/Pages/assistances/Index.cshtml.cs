using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.assistances
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IAppCache _cache;
        private readonly IRestApiService _api;

        public IndexModel(
            ILogger<IndexModel> logger,
            IAppCache cache,
            IRestApiService api)
        {
            _logger = logger;
            _cache = cache;
            _api = api;
        }

        public List<DistributionTypes> Distributions { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SelectedDistributionId { get; set; }

        public List<DistributionEnrollmentDto> DistributionEnrollment { get; set; } = new();

        // For details page
        [BindProperty(SupportsGet = true)]
        public int IndividualId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int DistributionId { get; set; }

        public DistributionEnrollmentDto? Enrollment { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? HouseholdId { get; set; }



        public async Task OnGetAsync()
        {
            // ===============================
            // DETAILS PAGE
            // ===============================
            if (IndividualId > 0 && DistributionId > 0)
            {
                await LoadEnrollmentByIds();
                return;
            }

            // ===============================
            // LIST PAGE
            // ===============================
            _logger.LogInformation("Assistances Index page loaded.");

            try
            {
                Distributions = await _api.GetAsync<List<DistributionTypes>>("v1/DistributionsType")
                                ?? new();

                Distributions = Distributions.Where(d => d.IsActive.HasValue && d.IsActive.Value)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching distributions.");
                Distributions = new();
            }

            try
            {
                DistributionEnrollment =
                    SelectedDistributionId > 0
                        ? await _api.GetAsync<List<DistributionEnrollmentDto>>(
                            $"v1/BeneficiaryEnrollments/{SelectedDistributionId}")
                        : await _api.GetAsync<List<DistributionEnrollmentDto>>(
                            "v1/BeneficiaryEnrollments")
                    ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching enrollments.");
                DistributionEnrollment = new();
            }
        }

        // Load distribution details
        private async Task LoadEnrollmentByIds()
        {
            try
            {
                Enrollment = await _api.GetAsync<DistributionEnrollmentDto>(
                    $"v1/BeneficiaryEnrollments/{DistributionId}/{IndividualId}/{HouseholdId}"
                );

                _logger.LogInformation(
                    "Details page load. DistributionId={DistributionId}, IndividualId={IndividualId}, HouseholdId={HouseholdId}, Found={Found}",
                    DistributionId,
                    IndividualId,
                    HouseholdId,
                    Enrollment != null
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading enrollment for Distribution {DistributionId}, Individual {IndividualId}, Household {HouseholdId}",
                    DistributionId,
                    IndividualId,
                    HouseholdId
                );

                Enrollment = null;
            }
        }

    }
}
