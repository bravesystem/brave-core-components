using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace BRaVe_Portal.Pages.Distributions
{
    public class DistributionsMappingModel : PageModel
    {
        private readonly ILogger<DistributionsMappingModel> _logger;
        private readonly IRestApiService _api;
        private readonly ILookupService _lookupService;
        public DistributionsMappingModel(
            ILogger<DistributionsMappingModel> logger,
            IRestApiService api,
            ILookupService lookupService)
        {
            _logger = logger;
            _api = api;
            _lookupService = lookupService;
        }
        [BindProperty(SupportsGet = true)]
        public int distributionId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Description { get; set; }
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? DistributionTitle { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? DistributionExternalId { get; set; }

        [BindProperty(SupportsGet = true)]
        public List<DistributionComposition> Distributions { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalRecords { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        [BindProperty]
        public string? SelectedItemIds { get; set; } = string.Empty;


        [BindProperty]
        public string SelectedIKits { get; set; }

        [BindProperty]
        public string? ItemIds { get; set; }

        [BindProperty]
        public int TenantId { get; set; }

        [BindProperty]
        public int Quantity { get; set; }

        [BindProperty]
        public decimal Measure { get; set; }

        [BindProperty]
        public int UoM { get; set; }


        [BindProperty]
        public int DistributionTypeId { get; set; }


        public List<DistributionItemTypes> Items { get; set; } = new();

        public List<DistributionComposition> FilteredDistributions { get; set; } = new();


        public long KitTypeId { get; set; }

        [BindProperty(SupportsGet = true)]
        public List<KitType> KitTypes { get; set; } = new();

        //public Dictionary<int, int> ItemCounts { get; set; } = new();


        [BindProperty]
        public DistributionComposition Distribution { get; set; } = new();

        [BindProperty]
        public DistributionComposition UpdatedDistribution { get; set; } = new();


        public async Task<IActionResult> OnGetAsync()
        {
            /*this.distributionId = distributionId;
            this.Description = Description;*/

            if (Request.ContainsPathProbeInQuery())
            {
                _logger.LogError("Blocked suspicious query payload on DistributionMapping GET.");
                return BadRequest("Invalid request.");
            }

            if (!Request.TryGetOptionalPositiveIntFromQuery(_logger, "distributionId", out var validatedDistributionId) ||
               !Request.TryGetSafeOptionalTextFromQuery(_logger, "Description", out var validatedDescription, 500) ||
               !Request.TryGetSafeOptionalTextFromQuery(_logger, "DistributionTitle", out var validatedDistributionTitle) ||
               !Request.TryGetSafeOptionalTextFromQuery(_logger, "DistributionExternalId", out var validatedDistributionExternalId))
            {
                _logger.LogError("Blocked suspicious query payload on DistributionMapping GET.");
                return BadRequest("Invalid request.");
            }

            distributionId = validatedDistributionId??0;
            Description = validatedDescription;
            DistributionTitle = validatedDistributionTitle;
            DistributionExternalId = validatedDistributionExternalId;


            try
            {
                await LoadLookups();

                // Load distributions
                Distributions = await _api.GetAsync<List<DistributionComposition>>($"v1/DistributionsComposition/by-distribution/{distributionId}") ?? new();

                // Load other data if needed
                Items = await _api.GetAsync<List<DistributionItemTypes>>("v1/DistributionsComposition/items") ?? new();

                KitTypes = await _api.GetAsync<List<KitType>>("v1/DistributionKitsType/dist_mapping") ?? new List<KitType>();

                // Optional search/filter
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    Distributions = Distributions
                        .Where(u =>
                            (!string.IsNullOrEmpty(u.UnitTitle) && u.UnitTitle.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(u.QRCode) && u.QRCode.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                        )
                        .ToList();
                }

                TotalRecords = Distributions.Count;

                // Calculate total pages safely
                var totalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);

                // Fix invalid page numbers
                if (CurrentPage < 1)
                    CurrentPage = 1;

                if (CurrentPage > totalPages)
                    CurrentPage = totalPages == 0 ? 1 : totalPages;

                FilteredDistributions = Distributions
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error loading distribution composition");

                TempData["DistErrorMessage"] = "Unable to load distribution composition/ Check if there are kits/items are linked to this distribution.";
            }

            return Page();


        }

        private async Task LoadLookups()
        {
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);
            ViewData["CoreLookups"] = coreLookups;
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddDistributionAsync()
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on AddDistribution POST.");
                return BadRequest("Invalid request.");
            }

            Distribution.TenantId = User.Tenant();

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Could not save Distribution. Please try again.");

                Distributions = await _api.GetAsync<List<DistributionComposition>>("v1/DistributionsComposition")
                    ?? new();

                return Page();
            }

            try
            {
                await _api.PostJsonAsync<DistributionComposition, object>("v1/DistributionsComposition", Distribution);

                TempData["DistSuccessMessage"] = "Distribution Added successfully.";

                await LoadLookups();
                return RedirectToPage("./DistributionsMapping", new { distributionId, Description });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add distribution");
                TempData["DistErrorMessage"] = "Failed to add distribution.";
                //ModelState.AddModelError(string.Empty, $"Error adding item: {ex.Message}");
                return RedirectToPage("./DistributionsMapping", new { distributionId, Description });
            }


        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddDistributionKitItemAsync()
        {
            if (Request.ContainsPathProbeInQuery() || 
                await Request.ContainsPathProbeInFormAsync() ||
                StringHelper.IsPotentialPathProbe(SelectedItemIds))
            {
                _logger.LogError("Blocked suspicious query payload on AddDistributionKitItem POST.");
                return BadRequest("Invalid request.");
            }

            try
            {
                // Validate selected item
                if (!long.TryParse(SelectedItemIds, out var itemId))
                {
                    ModelState.AddModelError(string.Empty, "Invalid item selected.");
                    return Page();
                }

                // Create DTO for API
                var dto = new DistributionItemCreateDto
                {
                    DistributionTypeId = distributionId, // hidden input
                    ItemTypeId = itemId,                 // selected item
                    Quantity = Quantity,                 // form field
                    Measure = Measure,                   // form field
                    UoM = UoM                            // form field
                };

                // Call API endpoint that expects List<DistributionItemCreateDto>
                var apiResponse = await _api.PostJsonAsync<List<DistributionItemCreateDto>, object>(
                    "v1/DistributionsComposition/AddDistributionItemAsync",
                    new List<DistributionItemCreateDto> { dto });

                TempData["DistSuccessMessage"] = "Item added successfully.";
                await LoadLookups();

                // here i need to reload this page with distributionId

                return RedirectToPage("./DistributionsMapping", new { distributionId, Description });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                TempData["DistErrorMessage"] = message;
                ModelState.AddModelError(string.Empty, $"Error adding item: {ex.Message}");
                return RedirectToPage("./DistributionsMapping", new { distributionId, Description });

            }
        }
        
        public record KitSelection(long Id, int Quantity);

      
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddKitItemAsync()
        {
            if (Request.ContainsPathProbeInQuery() || 
                await Request.ContainsPathProbeInFormAsync() ||
                StringHelper.IsPotentialPathProbe(SelectedIKits))
            {
                _logger.LogError("Blocked suspicious query payload on AddKitItem POST.");
                return BadRequest("Invalid request.");
            }

            try
            {
                var kits = new List<DistributionKitCreateDto>();

                if (!string.IsNullOrWhiteSpace(SelectedIKits))
                {
                    var entries = SelectedIKits.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    foreach (var entry in entries)
                    {
                        var parts = entry.Split(':');

                        kits.Add(new DistributionKitCreateDto
                        {
                            DistributionTypeId = distributionId,
                            KitTypeId = long.Parse(parts[0]),
                            Quantity = int.Parse(parts[1])

                        });
                    }
                }

                await _api.PostJsonAsync<List<DistributionKitCreateDto>, object>(
                    "v1/DistributionsComposition/kits/bulk",
                    kits);

                TempData["DistSuccessMessage"] = "Kits updated successfully.";

                return RedirectToPage("./DistributionsMapping", new { distributionId, Description });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while mapping Kits to distribution.");
                TempData["DistErrorMessage"] = "Error occured while mapping Kits to distribution.";
                //ModelState.AddModelError(string.Empty, ex.Message);
                return RedirectToPage("./DistributionsMapping", new { distributionId, Description });
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditDistributionAsync()
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on EditDistribution POST.");
                return BadRequest("Invalid request.");
            }

            //if (ContainsPathProbeInPayload(UpdatedDistribution))
            //{
            //    _logger.LogWarning("Blocked suspicious payload on EditDistribution POST.");
            //    return BadRequest("Invalid distribution payload.");
            //}

            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError(string.Empty, "Could not update Distribution. Please try again.");

                    Distributions = await _api.GetAsync<List<DistributionComposition>>("v1/DistributionsComposition")
                        ?? new();

                    return Page();
                }

                await _api.PutJsonAsync<DistributionComposition, object>(
                    $"v1/Distributions/{UpdatedDistribution.DistributionId}",
                    UpdatedDistribution);

                TempData["DistSuccessMessage"] = "Distribution updated successfully.";

                await LoadLookups();
                return RedirectToPage("./DistributionsMapping", new { distributionId, Description });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while mapping kit to distribution.");
                TempData["DistErrorMessage"] = "Error occured while mapping kit to distribution.";
                //ModelState.AddModelError(string.Empty, $"Error adding item: {ex.Message}");
                return RedirectToPage("./DistributionsMapping", new { distributionId, Description });

            }

        }

        public class InlineDistributionUpdateDto
        {
            public int Id { get; set; }
            public int Quantity { get; set; }
            public decimal Measure { get; set; }
            public int UoM { get; set; }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostUpdateInlineDistributionAsync([FromBody] InlineDistributionUpdateDto dto)
        {

            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on UpdateInlineDistribution POST.");
                Response.StatusCode = 400;
                return new JsonResult(new { success = false, message = "Invalid request." });
            }

            if (dto == null)
                return new JsonResult(new { success = false });

            try
            {
                var payload = new DistributionItemCreateDto
                {
                    ItemTypeId = dto.Id,
                    Measure = dto.Measure,
                    UoM = dto.UoM,
                    Quantity = dto.Quantity,
                };

                await _api.PutJsonAsync<DistributionItemCreateDto, object>(
                      $"v1/DistributionsComposition/inline_item_distribution/{dto.Id}",
                      payload);

                // TempData["DistSuccessMessage"] = "Item updated successfully.";
                return new JsonResult(new
                {
                    success = true,
                    message = "Distribution updated successfully"
                });

            }

            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occured while updating inline items for Distribution {dto.Id}.");
                //TempData["DistErrorMessage"] = "Failed to update Item.";
                return new JsonResult(new 
                { 
                    success = false, 
                    message = $"Failed to update items for Distribution {dto.Id}."
                });
            }
        }


        public class InlineKitDto
        {
            public int KitTypeId { get; set; }
            public int DistributionId { get; set; }
            public int Quantity { get; set; }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostUpdateInlineKitAsync([FromBody] InlineKitDto dto)
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on UpdateInlineKit POST.");
                Response.StatusCode = 400;
                return new JsonResult(new { success = false, message = "Invalid request." });
            }


            if (dto == null)
                return new JsonResult(new { success = false });

            try
            {
                var payload = new DistributionKitCreateDto
                {
                    KitTypeId = dto.KitTypeId,
                    DistributionTypeId = dto.DistributionId, // mapping happens here
                    Quantity = dto.Quantity
                };

                await _api.PutJsonAsync<DistributionKitCreateDto, object>(
                    $"v1/DistributionsComposition/kit-inline",
                    payload);

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inline kit update failed for KitTypeId {KitTypeId}", dto.KitTypeId);
                return new JsonResult(new 
                { 
                    success = false,
                    message = $"Failed to update kit {dto.KitTypeId}."
                });
            }
        }
    }
}