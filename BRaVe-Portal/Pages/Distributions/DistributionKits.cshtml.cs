using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BRaVe_Portal.Pages.Distributions
{
    public class DistributionKitsModel : PageModel
    {
        private readonly ILogger<DistributionKitsModel> _logger;
        private readonly IRestApiService _api;

        public DistributionKitsModel(
            ILogger<DistributionKitsModel> logger,
            IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public List<KitType> KitTypes { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalRecords { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public List<KitType> FilteredKitTypes { get; set; } = new();

        [BindProperty]
        public KitType Kit { get; set; } = new();

   
        private List<SelectListItem> SourceList { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "I", Text = "Internal" },
            new SelectListItem { Value = "E", Text = "External" }
        };


        public async Task<IActionResult> OnGetAsync()
        {

            if ( Request.ContainsPathProbeInQuery() ||
               StringHelper.IsPotentialPathProbe(SearchTerm) )
            {
                _logger.LogError("Blocked suspicious search term pattern on KitItems page.");
                return BadRequest("Invalid request.");
            }

            try
            {
                ViewData["SourceList"] = SourceList;

                var data = await _api.GetAsync<List<KitType>>("v1/DistributionKitsType")
                           ?? new List<KitType>();

                // SEARCH
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    CurrentPage = 1;

                    data = data.Where(k =>
                            (!string.IsNullOrEmpty(k.Name) && k.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(k.ExternalId) && k.ExternalId.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                        )
                        .ToList();
                }

                // SORT
                data = data
                    .OrderByDescending(k => k.UpdatedOn ?? k.CreatedOn)
                    .ToList();

                // TOTAL
                TotalRecords = data.Count;

                // PAGE GUARD
                if (CurrentPage < 1)
                    CurrentPage = 1;

                if (CurrentPage > TotalPages)
                    CurrentPage = TotalPages == 0 ? 1 : TotalPages;

                // PAGING
                FilteredKitTypes = data
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                KitTypes = data;

                return Page();
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "API failure while loading existing kits");
                TempData["ErrorMessage"] = "Unable to load kit list. Please try again.";

                return RedirectToPage("/DistributionsType");
            }
        }

       [ValidateAntiForgeryToken]
       public async Task<IActionResult> OnPostSaveKitTypeAsync()
       {

            if (Request.ContainsPathProbeInQuery() ||
                await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on SaveKitType POST.");
                return BadRequest("Invalid request.");
            }

            try
            {
                ModelState.Remove("Kit.CreatedByUserId");
                ModelState.Remove("Kit.CreatedOn");
                ModelState.Remove("Kit.UpdatedByUserId");
                ModelState.Remove("Kit.UpdatedOn");

                // Add audit info
                Kit.TenantId = User.Tenant();
                Kit.CreatedOn = DateTime.UtcNow;
                Kit.CreatedByUserId = User.Identity?.Name ?? "system";
                Kit.UpdatedOn = DateTime.UtcNow;
                Kit.UpdatedByUserId = User.Identity?.Name ?? "system";

                if (!ModelState.IsValid)
                {
                    KitTypes = await _api.GetAsync<List<KitType>>("v1/DistributionKitsType") ?? new();
                    return Page();
                }

                if (Kit.Id > 0)
                {
                    await _api.PutJsonAsync<KitType, object>(
                        $"v1/DistributionKitsType/{Kit.Id}",
                        Kit);
                    TempData["DistributionMessage"] = "Kit updated successfully.";

                    TempData["DistributionStatus"] = "success";

                }
                else
                {
                    await _api.PostJsonAsync<KitType, object>(
                        "v1/DistributionKitsType",
                        Kit);
                  
                    TempData["DistributionMessage"] = "Kit created successfully.";

                    TempData["DistributionStatus"] = "success";

                }

            }
            catch (Exception ex)
            {
                //TempData["DistributionMessage"] = ex.Message;

                _logger.LogError(ex, "API failure while loading existing kits");

                TempData["DistributionMessage"] = "Unable to load kit list.";

                TempData["DistributionStatus"] = "error";

                KitTypes = await _api.GetAsync<List<KitType>>("v1/DistributionKitsType") ?? new();

            }

            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteKitTypeAsync(int id)
        {
            if (Request.ContainsPathProbeInQuery() ||
                await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on DeleteKitType POST.");
                return BadRequest("Invalid request.");
            }

            try
            {
                await _api.DeleteAsync($"v1/DistributionKitsType/{id}");
                
            }
            catch (Exception ex) {

                _logger.LogError(ex, $"API failure while deleting kit {id}");

                TempData["DistributionMessage"] = "Unable to delete kit.";

                TempData["DistributionStatus"] = "error";

            }

            return RedirectToPage();

        }
    }
}