using Azure;
using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Models.Enums;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2013.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace BRaVe_Portal.Pages.Distributions.KitItems
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        //private readonly IAppCache _cache;

        private readonly IRestApiService _api;

        private readonly ILookupService _lookupService;

        public IndexModel(ILogger<IndexModel> logger, /*IAppCache cache,*/ IRestApiService api, ILookupService lookupService)
        {
            _logger = logger;
            //_cache = cache;
            _api = api;
            _lookupService = lookupService;
        }
        [BindProperty(SupportsGet = true)]
        public string? Source { get; set; }

        [BindProperty(SupportsGet = true)]
        public int distributionId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Description { get; set; }

        [BindProperty(SupportsGet = true)]
        public int KitID { get; set; }

        [BindProperty]
        public string? UoMExternalCode { get; set; }


        [BindProperty(SupportsGet = true)]
        public string? KitName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? QrCode { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Note { get; set; }
        [BindProperty]
        public int TenantId { get; set; }

        [BindProperty]
        public int Quantity { get; set; }

        [BindProperty]
        public decimal Measure { get; set; }

        [BindProperty]
        public int UoM { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? KitExternalId { get; set; }



        [BindProperty(SupportsGet = true)]
        public string? KitSKU { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public string? DisplaySearchTerm { get; private set; }

        [BindProperty(SupportsGet = true)]
        //public List<DistributionKit> DistributionKits { get; private set; } = new();
        public List<KitItemsDto> DistributionKitItems { get; private set; } = new();

  
        public List<DistributionItemTypes> Items { get; set; } = new();

        [BindProperty]
        public string SelectedItemIds { get; set; } = string.Empty;


        public async Task<IActionResult> OnGetAsync()
        {

            if (Request.ContainsPathProbeInQuery() || 
                StringHelper.IsPotentialPathProbe(SearchTerm))
            {
                _logger.LogError("Blocked suspicious search term pattern on KitItems page.");
                return BadRequest("Invalid request.");
            }

            DisplaySearchTerm = SearchTerm;

            try
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                int id = KitID;
                ViewData["KitID"] = KitID;

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);
                ViewData["CoreLookups"] = coreLookups;

                var KitNamei = KitName;
                TempData["KitName"] = "Kit Name:" + " " + KitName;
                TempData["KitSKU"] = "SKU (Stock Keeping Unit):" + " " + KitSKU;

                DistributionKitItems = await _api.GetAsync<List<KitItemsDto>>(
                $"v1/DistributionsComposition/by-kit/{KitID}") ?? new();
                var kit = DistributionKitItems.FirstOrDefault(x => x.KitID == KitID);
                if (kit != null)
                {
                    Note = kit.Notes;
                    KitExternalId = kit.UoMExternalCode;
                }
                Items = await _api.GetAsync<List<DistributionItemTypes>>("v1/DistributionsComposition/items") ?? new();

                var DistributionKitItemss = DistributionKitItems.Where(x => x.KitID.Equals(KitID)).ToList();


                var DistributionKitItemList = DistributionKitItems.Where(x => x.KitID.Equals(KitID)).ToList();

                if (string.IsNullOrEmpty(Source))
                {
                    Source = TempData["Source"]?.ToString();
                }
                else
                {
                    TempData["Source"] = Source;
                }

                TempData.Keep("Source");
                if (string.IsNullOrEmpty(Description))
                {
                    Description = TempData["Description"]?.ToString();
                }
                else
                {
                    TempData["Description"] = Description;
                }

                TempData.Keep("Description");

                if (distributionId == 0)
                {
                    distributionId = TempData["distributionId"] != null
                        ? Convert.ToInt32(TempData["distributionId"])
                        : 0;
                }
                else
                {
                    TempData["distributionId"] = distributionId;
                }

                TempData.Keep("distributionId");

                // Filter by search term
                if (!string.IsNullOrWhiteSpace(DisplaySearchTerm))
                {
                    CurrentPage = 1; // reset page on search
                    DistributionKitItemList = DistributionKitItemList
                        .Where(u => u.ItemName.Contains(DisplaySearchTerm, StringComparison.OrdinalIgnoreCase)
                          || u.ItemTag.Contains(DisplaySearchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                TotalRecords = DistributionKitItemList.Count;

                // Apply paging
                FilteredUsers = DistributionKitItemList
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                return Page();

            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "API failure while loading items linked to kit {KitID}", KitID);
                TempData["ErrorMessage"] = "Unable to load the item list for the selected kit. Please try again.";

                return RedirectToPage("/Distributions/DistributionKits");

            }
        
        }
 
        [BindProperty]
        public DistributionKitItemDto DistributionKitItem { get; set; } = new();

        public List<KitItemsDto> FilteredUsers { get; set; } = new();

        [BindProperty]
        public DistributionKitItem UpdatedDistributionItemKit { get; set; } = new();

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddKitItem()
        {

            if (Request.ContainsPathProbeInQuery() || 
                await Request.ContainsPathProbeInFormAsync() ||
                StringHelper.IsPotentialPathProbe(SelectedItemIds))
            {
                _logger.LogError("Blocked suspicious query payload on AddKitItem POST.");
                return BadRequest("Invalid request.");
            }

            try
            {
                if (string.IsNullOrWhiteSpace(SelectedItemIds))
                {
                    ModelState.AddModelError(string.Empty, "No items selected.");
                    return RedirectToPage(new { KitID });
                }

                var items = SelectedItemIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x =>
                    {
                        var parts = x.Split(':');

                        if (parts.Length < 3)
                            throw new Exception($"Invalid item format: {x}");

                        return new KitItemInsertDto
                        {
                            ItemId = long.Parse(parts[0]),
                            Measure = decimal.Parse(parts[1]),
                            UoM = int.Parse(parts[2]),
                            TenantId = User.Tenant(),
                            KitId = KitID
                        };
                    })
                    .ToList();

                _logger.LogInformation("Adding {Count} items to KitId {KitId}", items.Count, KitID);

                var response = await _api.PostJsonAsync<List<KitItemInsertDto>, object>(
                    "v1/DistributionsComposition/items/bulk", items);

                TempData["ItemSuccessMessage"] = "Items added successfully.";
                return RedirectToPage(new { KitID, distributionId, Description, Source });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding items to KitId {KitId}. Payload: {Payload}", KitID, SelectedItemIds);

                // Extract clean message (important for API errors)
                var message = "Error occured while adding items to kit. Please try again.";
                    //ex.InnerException?.Message ?? ex.Message;
                TempData["ItemErrorMessage"] = message;

                ModelState.AddModelError(string.Empty, message);

                return RedirectToPage(new { KitID, distributionId, Source });
            }
        }

        public class InlineUpdateDto
        {
            public int Id { get; set; }
            public decimal Measure { get; set; }
            public int UoM { get; set; }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostUpdateInlineAsync([FromBody] InlineUpdateDto dto)
        {
            if (dto == null)
                return new JsonResult(new { success = false });

            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on UpdateInlineAsync POST.");
                return new JsonResult(new { success = false });
            }

            try
            {
                var payload = new KitItemInsertDto
                {
                    ItemId = dto.Id,
                    Measure = dto.Measure,
                    UoM = dto.UoM,
                    TenantId = User.Tenant()
                };

                await _api.PutJsonAsync<KitItemInsertDto, object>(
              $"v1/DistributionsComposition/inline/{dto.Id}",   
              payload);

                //TempData["ItemSuccessMessage"] = "Item updated successfully.";
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                //TempData["ItemErrorMessage"] = "Failed to update Item.";
                _logger.LogError(ex, "Inline update failed for Id {Id}", dto.Id);
                return new JsonResult(new { success = false });
            }
        }

    }
}
