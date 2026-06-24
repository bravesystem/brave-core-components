using BRaVe_Portal.Extensions;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace BRaVe_Portal.Pages.DuplicateRulesets
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        private readonly IRestApiService _api;

        public IndexModel(ILogger<IndexModel> logger, IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }
        public List<DuplicateRulesetViewModel> DuplicateRulesets { get; private set; } = new();

        public async Task<IActionResult> OnGet()
        {
            if (Request.ContainsPathProbeInQuery())
            {
                _logger.LogError("Blocked suspicious query pattern on DuplicateRulesets page.");
                return BadRequest("Invalid request.");
            }

            try
            {
                DuplicateRulesets = await _api.GetAsync<List<DuplicateRulesetViewModel>>("v1/DuplicateRulesets")
                    ?? new List<DuplicateRulesetViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while fetching duplicate ruleset list.");
                TempData["ErrorMessage"] = "Error occured while fetching duplicate ruleset list.";
                DuplicateRulesets = new List<DuplicateRulesetViewModel>();
            }

            return Page();
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAdd() {

            if (Request.ContainsPathProbeInQuery() ||
               await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on Add Ruleset POST.");
                return BadRequest("Invalid request.");
            }


            TempData["ErrorMessage"] = "You cannot add a custom ruleset for now; you must use the default (Built-In) rulesets.";

            return await OnGet();
        
        }
    }
}
