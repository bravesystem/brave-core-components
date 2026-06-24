using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace BRaVe_Portal.Pages.Distributions.Items
{
    public class DistributionItemsModel : PageModel
    {
        private readonly ILogger<DistributionItemsModel> _logger;
        private readonly IAppCache _cache;
        private readonly IRestApiService _api;
        private readonly ILookupService _lookupService;

        public DistributionItemsModel(
            ILogger<DistributionItemsModel> logger,
            IAppCache cache,
            IRestApiService api,
            ILookupService lookupService)
        {
            _logger = logger;
            _cache = cache;
            _api = api;
            _lookupService = lookupService;
        }

        [BindProperty(SupportsGet = true)]
        public string? KitName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? KitSKU { get; set; }

        // FIX: Must support GET for pagination links
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalRecords { get; set; }

        // Computed property (do not assign to it)
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public List<DistributionItemTypes> Items { get; set; } = new();
        public List<DistributionItemTypes> FilteredItems { get; set; } = new();

        [BindProperty]
        public DistributionItemTypes UpdatedItem { get; set; } = new();

        [BindProperty]
        public bool IsActive { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (Request.ContainsPathProbeInQuery() ||
                StringHelper.IsPotentialPathProbe(KitName) ||
                StringHelper.IsPotentialPathProbe(KitSKU) )
            {
                _logger.LogWarning("Blocked suspicious search term pattern on DistributionItems page.");
                return BadRequest("Invalid request.");
            }

            try
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);
                ViewData["CoreLookups"] = coreLookups;

                TempData["KitName"] = "Kit Name: " + KitName;
                TempData["KitSKU"] = "SKU (Stock Keeping Unit): " + KitSKU;

                Items = await _api.GetAsync<List<DistributionItemTypes>>("v1/DistributionsComposition/items")
                    ?? new List<DistributionItemTypes>();
                Items = Items
                    .OrderByDescending(i => i.UpdatedOn ?? i.CreatedOn)
                    .ToList();
                // SEARCH
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    Items = Items.Where(i =>
                            (!string.IsNullOrEmpty(i.Name) &&
                             i.Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                            ||
                            (!string.IsNullOrEmpty(i.SKU) &&
                             i.SKU.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                        ).ToList();
                }

                // TOTAL RECORDS
                TotalRecords = Items.Count;

                // ENSURE CURRENT PAGE IS VALID
                if (CurrentPage < 1)
                    CurrentPage = 1;

                if (CurrentPage > TotalPages)
                    CurrentPage = TotalPages == 0 ? 1 : TotalPages;

                // PAGINATION
                FilteredItems = Items
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API failure while loading existing items");
                TempData["ErrorMessage"] = "Unable to load item list. Please try again.";

                return RedirectToPage("/DistributionsType");
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSaveDistributionItem()
        {
            if (Request.ContainsPathProbeInQuery() ||
                await Request.ContainsPathProbeInFormAsync() )
            {
                _logger.LogWarning("Blocked suspicious query payload on SaveDistributionItem POST.");
                return BadRequest("Invalid request.");
            }


            try
            {
                if (UpdatedItem.Id == 0)
                {
                    await _api.PostJsonAsync<DistributionItemTypes, object>(
                        "v1/DistributionsComposition",
                        UpdatedItem);
                    TempData["ItemsMessage"] = "Item created successfully.";
                    TempData["ItemStatus"] = "success";
                }
                else
                {
                    await _api.PutJsonAsync<DistributionItemTypes, object>(
                        $"v1/DistributionsComposition/{UpdatedItem.Id}",
                        UpdatedItem);
                    TempData["ItemsMessage"] = "Kit updated successfully.";
                    TempData["ItemStatus"] = "success";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving DistributionItem with Id {Id}", UpdatedItem?.Id);
                TempData["ItemsMessage"] = "Error occured.";
                TempData["ItemStatus"] = "error";
            }

            return RedirectToPage(new
            {
                CurrentPage,
                PageSize,
                SearchTerm
            });
        }
    }
}