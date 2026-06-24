using BRaVe_Portal.Extensions;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.DuplicateRulesets
{
    public class DuplicatePredicatesModel : PageModel
    {

        private readonly ILogger<IndexModel> _logger;

        private readonly IRestApiService _api;

        public DuplicatePredicatesModel(ILogger<IndexModel> logger, IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        public int RulesetId { get; set; }
        public string RulesetName { get; set; } = string.Empty;
        public List<DuplicatePredicates> Predicates { get; private set; } = new();

        //
        public async Task<IActionResult> OnGet(int RulesetId)
        {
            if (Request.ContainsPathProbeInQuery())
            {
                _logger.LogError("Blocked suspicious query pattern on DuplicatePredicates page.");
                return BadRequest("Invalid request.");
            }

            if (RulesetId == 0)
            {
                return RedirectToPage("/DuplicateRulesets");
            }
            try
            {
                var PredicatesVM = await _api.GetAsync<DuplicatePredicateViewModel>($"v1/DuplicateRulesets/predicates/{RulesetId}")
                   ?? new DuplicatePredicateViewModel();

                Predicates = PredicatesVM.Predicates;

                RulesetId = PredicatesVM.RulesetId;
                RulesetName = PredicatesVM.RulesetName;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while fetching duplicate predicate list.");
                TempData["ErrorMessage"] = "Error occured while fetching duplicate predicate list."; ;
            }


            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAdd(int RulesetId)
        {

            if (Request.ContainsPathProbeInQuery() ||
               await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on Add Predicate POST.");
                return BadRequest("Invalid request.");
            }

            TempData["ErrorMessage"] = "Custom predicates will be available in a future version; you are currently limited to the existing ones.";

            return await OnGet(RulesetId);

        }
    }
}
