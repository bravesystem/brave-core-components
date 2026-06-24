using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.dashboard.Raw_Data
{
    public class SurveyDetailsModel : PageModel
    {
        private readonly IRestApiService _api;
        private readonly ILogger<SurveyDetailsModel> _logger;

        public SurveyDetailsModel(IRestApiService api, ILogger<SurveyDetailsModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string Type { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string? HouseholdId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? IndividualId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Name { get; set; }

 

        public List<DashboardSurveyDetailDto> surveyDetail { get; set; }


        public List<DashboardSurveyDetailDto> SurveyDetails { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (Type.Equals("household", StringComparison.OrdinalIgnoreCase))
            {
                IndividualId = null;
            }

            SurveyDetails = await _api.GetAsync<List<DashboardSurveyDetailDto>>(
                $"v1/dashboard/survey-details?householdId={HouseholdId}&individualId={IndividualId}")
                ?? new();
        }

    }

}
