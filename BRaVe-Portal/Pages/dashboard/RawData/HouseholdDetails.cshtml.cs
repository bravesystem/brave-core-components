using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.Dashboard
{
    public class HouseholdModel : PageModel
    {
        private readonly ILogger<HouseholdModel> _logger;
        private readonly IRestApiService _api;

        public HouseholdModel(
            ILogger<HouseholdModel> logger,
            IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        // Route param
        [BindProperty(SupportsGet = true)]
        public string HouseholdId { get; set; } = string.Empty;

        // Household header
        public HouseholdDto Household { get; set; } = new();

        // Household members (NEW DTO)
        public List<HouseholdMemberDto> Members { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(HouseholdId))
                return RedirectToPage("/Dashboard/Index");

            try
            {
                // Load household header
                var householdTask = _api.GetAsync<HouseholdDto>(
                    $"v1/dashboard/households/{HouseholdId}");

                // Load household members (NEW ENDPOINT)
                var membersTask = _api.GetAsync<List<HouseholdMemberDto>>(
                    $"v1/dashboard/households/{HouseholdId}/members");

                await Task.WhenAll(householdTask, membersTask);

                Household = householdTask.Result
                    ?? new HouseholdDto { HouseholdId = HouseholdId };

                Members = membersTask.Result
                    ?? new List<HouseholdMemberDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed loading household profile for HouseholdId {HouseholdId}",
                    HouseholdId);
            }

            return Page();
        }
    }
}
