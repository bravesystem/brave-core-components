using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;

namespace BRaVe_Portal.Pages.Locations
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        //private readonly IAppCache _cache;
        private readonly IRestApiService _api;

        public IFormFile ExcelFile { get; set; }

        public IndexModel(
            ILogger<IndexModel> logger,
            //IAppCache cache,
            IRestApiService api)
        {
            _logger = logger;
            //_cache = cache;
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public List<Location> Locations { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public List<AdministrativeLevel> AdministrativeLevels { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public string? DisplaySearchTerm { get; private set; }

        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        //[BindProperty]
        //public LocationDto Location { get; set; } = new();

        [BindProperty]
        public LocationDto addLocation { get; set; } = new();


        [BindProperty]
        public Location UpdatedLocation { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            
            if (Request.ContainsPathProbeInQuery() || 
                StringHelper.IsPotentialPathProbe(SearchTerm))
            {
                _logger.LogWarning("Blocked suspicious  query payload on Location GET");
                return BadRequest("Invalid request.");
            }

            DisplaySearchTerm = SearchTerm;

            var tenantId = User.Tenant();

            _logger.LogInformation(
                "Loading Location Management page. TenantId={TenantId}, Page={Page}, PageSize={PageSize}, SearchTerm={DisplaySearchTerm}",
                tenantId, PageNumber, PageSize, DisplaySearchTerm);

            int step = 1;

            try
            {
                Locations = await _api.GetAsync<List<Location>>("v1/Locations") ?? new();
                _logger.LogInformation("Fetched {Count} locations.", Locations.Count);

                step++;

                AdministrativeLevels =
                await _api.GetAsync<List<AdministrativeLevel>>("v1/AdministrativeLevels")
                ?? new();

                _logger.LogInformation("Fetched {Count} administrative levels.", AdministrativeLevels.Count);

                step++;

                if (!string.IsNullOrWhiteSpace(DisplaySearchTerm))
                {
                    var before = Locations.Count;

                    Locations = Locations
                         .Where(l =>
                             (!string.IsNullOrEmpty(l.LocationName) &&
                              l.LocationName.Contains(DisplaySearchTerm, StringComparison.OrdinalIgnoreCase))
                             ||
                             (!string.IsNullOrEmpty(l.OfficialCode) &&
                              l.OfficialCode.Contains(DisplaySearchTerm, StringComparison.OrdinalIgnoreCase))
                         )
                         .ToList();


                    _logger.LogInformation(
                        "Search applied. Before={Before}, After={After}",
                        before, Locations.Count);

                    PageNumber = 1;
                }

                TotalRecords = Locations.Count;

                Locations = Locations
                    .OrderByDescending(l => l.UpdatedOn ?? DateTime.MinValue)
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                _logger.LogInformation(
                    "Paging applied. Returned={Count}, Page={Page}",
                    TotalRecords, PageNumber);

                
            }
            catch(Exception ex)
            {
                //_logger.LogInformation("Could not pass locations");

                if (step == 1)
                {
                    _logger.LogError(ex, "API failure while loading locations");
                    TempData["ErrorMessage"] = "Unable to load the location list. Please try again.";
                }
                else if (step == 2)
                {
                    _logger.LogError(ex, "API failure while loading administrative levels");
                    TempData["ErrorMessage"] = "Unable to load the administrative level list. Please try again.";
                }
                else
                {
                    _logger.LogError(ex, "API failure during data loading. Unknown step: {Step}", step);
                    TempData["ErrorMessage"] = "An unexpected error occurred while loading data. Please try again later.";
                }
               
            }

            return Page();

        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddLocationAsync()
        {

            if (Request.ContainsPathProbeInQuery() ||
               await Request.ContainsPathProbeInFormAsync() )
            {
                _logger.LogError("Blocked suspicious query payload on AddLocation POST.");
                return BadRequest("Invalid request.");
            }

            ModelState.Remove(nameof(ExcelFile));
            ModelState.Remove(nameof(UpdatedLocation));
            ModelState.Remove("LocationName");     
            ModelState.Remove("OfficialCode");

            addLocation.TenantId = User.Tenant();

            if (addLocation.ParentLocationId == 0)
                addLocation.ParentLocationId = null;

            _logger.LogInformation(
                "Add location request. Name={Name}, LevelId={LevelId}, TenantId={TenantId}",
                addLocation.LocationName, addLocation.LevelId, addLocation.TenantId);


            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Add Administrative Area validation failed.");
                TempData["ErrorMessage"] = "Error adding new Administrative Area";
                return await OnGetAsync();
            }

            try
            {
                await _api.PostJsonAsync<LocationDto, object>(
                        "v1/Locations",
                        addLocation
                    );

                _logger.LogInformation("Administrative Area added successfully.");

                TempData["SuccessMessage"] = "New Administrative Area added successfully";

            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error occured while addting New Administrative Area");

                TempData["ErrorMessage"] = "Error occured while adding New Administrative Area";

            }
            

            return RedirectToPage();
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditLocationAsync()
        {

            if (Request.ContainsPathProbeInQuery() ||
               await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on EditLocation POST.");
                return BadRequest("Invalid request.");
            }

            ModelState.Remove(nameof(ExcelFile));
            ModelState.Remove(nameof(addLocation));
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning(
                    "Edit Administrative Area validation failed. LocationId={Id}",
                    UpdatedLocation.Id);

                TempData["ErrorMessage"] = "An error occured when editing Administrative Area";
                return Page();
            }

            var dto = new LocationDto
            {
                LocationName = UpdatedLocation.LocationName,
                OfficialCode = UpdatedLocation.OfficialCode,
                UpdatedOn = UpdatedLocation.UpdatedOn,
                ParentLocationId = UpdatedLocation.ParentLocationId,
                LevelId = UpdatedLocation.LevelId,
                IsActive = UpdatedLocation.IsActive,
                TenantId = User.Tenant()
            };


            try
            {
                await _api.PutJsonAsync<LocationDto, object>(
                $"v1/Locations/{UpdatedLocation.Id}", dto);

                TempData["SuccessMessage"] = "Administrative Area edited successfully";
                
                _logger.LogInformation(
                    "Administrative Area updated successfully. LocationId={Id}",
                    UpdatedLocation.Id);
            }
            catch( Exception e)
            {
                _logger.LogError(e, "Location not updated . LocationId={Id}", UpdatedLocation.Id);

                TempData["ErrorMessage"] = "An error occured when editing location";
                
            }
           

            return RedirectToPage();
        }


     
        public async Task<JsonResult> OnGetParentLocationsAsync(int levelId)
        {
            _logger.LogInformation(
                "Fetching parent Administrative Area. LevelId={LevelId}",
                levelId);

            if (Request.ContainsPathProbeInQuery() )
            {
                _logger.LogError("Blocked suspicious  query payload on Location GET");
                return new JsonResult(new List<SelectListItem>());
            }

            var locations = await _api.GetAsync<List<Location>>("v1/Locations") ?? new();

            var parents = locations
                .Where(x => x.LevelId == levelId && x.IsActive)
                .Select(x => new
                {
                    id = x.Id,
                    locationName = x.LocationName
                })
                .ToList();

            _logger.LogInformation(
                "Returning {Count} parent Administrative Area for LevelId={LevelId}",
                parents.Count, levelId);

            return new JsonResult(parents);
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostUploadExcelAdministrativeLevelLocationAsync()
        {
            _logger.LogInformation("Starting Upload Administrative Area process at {Time}.", DateTime.UtcNow);

            if (Request.ContainsPathProbeInQuery() ||
              await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on UploadExcelAdministrativeLevelLocation POST.");
                return BadRequest("Invalid request.");
            }

            if (ExcelFile == null || ExcelFile.Length == 0)
            {
                TempData["UploadErrorMessage"] = "Please select a valid Excel file before uploading.";
                return RedirectToPage();
            }

            //  TEMPLATE VALIDATION 
            try
            {
                using var stream = new MemoryStream();
                await ExcelFile.CopyToAsync(stream);
                stream.Position = 0;

                using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();

                var headerRow = worksheet.Row(1);

                var expectedHeaders = new[]
                {
                    "ID", "Name", "Level", "Parent", "OfficialCode", "IsActive"
                };

                for (int i = 0; i < expectedHeaders.Length; i++)
                {
                    var cellValue = headerRow.Cell(i + 1).GetString()?.Trim();

                    if (!string.Equals(cellValue, expectedHeaders[i], StringComparison.OrdinalIgnoreCase))
                    {
                        TempData["ErrorMessage"] = "Upload failed. Please download the correct template.";
                        return RedirectToPage();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid Excel format uploaded.");
                TempData["ErrorMessage"] = "Invalid or corrupted Excel file. Please use the correct template.";
                return RedirectToPage();
            }

            // PROCEED WITH API CALL
            try
            {
                var response = await _api.PostFileAsync(
                    "v1/Locations/upload-excelAdministrativeLevelLocation",
                    ExcelFile
                );

                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var result = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(content);
                        var message = result != null && result.ContainsKey("message")
                            ? result["message"]
                            : "Upload completed successfully.";

                        TempData["SuccessMessage"] = message;
                    }
                    catch
                    {
                        TempData["SuccessMessage"] = "Upload completed successfully.";
                    }
                }
                else
                {
                    try
                    {
                        var error = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(content);
                        var message = error != null && error.ContainsKey("message")
                            ? error["message"]
                            : "Upload failed.";

                        TempData["ErrorMessage"] = message;
                    }
                    catch
                    {
                        TempData["ErrorMessage"] = $"Upload failed: {content}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during upload.");
                TempData["ErrorMessage"] = "An unexpected error occurred while uploading the file.";
            }

            return RedirectToPage();
        }


        //Download Excel template


        public async Task<IActionResult> OnGetDownloadExcelAdministrativeLevelLocationAsync()
        {

            if (Request.ContainsPathProbeInQuery())
            {
                _logger.LogError("Blocked suspicious  query payload on DownloadExcelAdministrativeLevelLocation GET");
                return BadRequest("Invalid request.");
            }

            try
            {
                var bytes = await _api.GetFileAsync(
                    "v1/Locations/download-excelAdministrativeLevelLocation"
                );

                var fileName = $"Administrative_Level_Location_Template_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

                return File(bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading template.");
                TempData["ErrorMessage"] = "Error downloading template.";
                return RedirectToPage();
            }
        }
    }
}
