using BRaVe_Management_Backend.DTOs;
using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace BRaVe_Portal.Pages.AdministrativeLevels
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        private readonly IAppCache _cache;

        private readonly IRestApiService _api;
        [BindProperty(SupportsGet = true)]

        public IFormFile? ExcelFile { get; set; } = null;

        public IndexModel(ILogger<IndexModel> logger, IAppCache cache, IRestApiService api)
        {
            _logger = logger;
            _cache = cache;
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public List<AdministrativeLevel> AdministrativeLevels { get; private set; } = new();
        public List<AdministrativeLevel> FilteredUsers { get; set; } = new();

     

        public async Task OnGetAsync()
        {
            AdministrativeLevels = await _api
                .GetAsync<List<AdministrativeLevel>>("/api/v1/AdministrativeLevels")
                ?? new List<AdministrativeLevel>();

            // Search
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                AdministrativeLevels = AdministrativeLevels
                    .Where(x =>
                        x.LevelName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        x.OfficialCode.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Sort BEFORE paging
            AdministrativeLevels = AdministrativeLevels
                .OrderByDescending(x => x.UpdatedOn)
                .ToList();

            TotalRecords = AdministrativeLevels.Count;

            // Paging
            FilteredUsers = AdministrativeLevels
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }


        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        [BindProperty]
        public AdministrativeLevelDto AdministrativeLevel { get; set; } = new();
     

        [BindProperty]
        public AdministrativeLevel UpdatedAdministrativeLevel { get; set; } = new();


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddAdministrativeLevelAsync()
        {

            ModelState.Remove(nameof(ExcelFile));
            ModelState.Remove(nameof(SearchTerm));
            AdministrativeLevel.TenantId = User.Tenant();
            if(AdministrativeLevel.OfficialCode==null)
            {
                AdministrativeLevel.OfficialCode = "";
            }
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Could not save Administrative Level  . Please try again.");
                TempData["ErrorMessage"] = "Could not add administrator level.";
                return Page();
            }


            try
            {
                await _api.PostJsonAsync<AdministrativeLevelDto, object>("v1/AdministrativeLevels", AdministrativeLevel);

                TempData["SuccessMessage"] = "Administrator level added sucessfully";

                await _cache.RemoveAsync(StaticKeyNames.ALL_ENUMERATORS);
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = "Could not add administrator level. Check if a similar id exists";
            }
           

            return RedirectToPage();

        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditAdministrativeLevelAsync()
        {

            if (AdministrativeLevel.OfficialCode == null)
            {
                AdministrativeLevel.OfficialCode = "";
            }
            ModelState.Remove(nameof(ExcelFile));
            ModelState.Remove(nameof(SearchTerm));
            if (!ModelState.IsValid)
            {
                AdministrativeLevel.TenantId = User.Tenant();
                ModelState.AddModelError(string.Empty, "Could not update AdministrativeLevel. Please try again.");
                AdministrativeLevels= await _api.GetAsync<List<AdministrativeLevel>>("v1/AdministrativeLevels") ?? new();
                return Page();
            }

            AdministrativeLevelDto administrativeLevelDto = new AdministrativeLevelDto()
            {
                
            LevelName = UpdatedAdministrativeLevel.LevelName,
                OfficialCode = UpdatedAdministrativeLevel.OfficialCode,
                UpdatedOn = UpdatedAdministrativeLevel.UpdatedOn,
                IsActive = UpdatedAdministrativeLevel.IsActive,
                  TenantId = User.Tenant(),
            };
            //await _api.PutJsonAsync<EnumeratorDto, object>("v1/enumerators", Enumerator);
            await _api.PutJsonAsync<AdministrativeLevelDto, object>($"v1/AdministrativeLevels/{UpdatedAdministrativeLevel.Id}", administrativeLevelDto);

            //try {
            //    //Regions = await _api.GetAsync<List<Region>>("v1/regions") ?? new();
            //    await _api.PutJsonAsync<EnumeratorDto, object>($"v1/Enumerators"); }

            //catch (Exception ex)
            //{
            //}

            await _cache.RemoveAsync(StaticKeyNames.ALL_ENUMERATORS);

            await _cache.RemoveAsync($"{StaticKeyNames.ENUMERATOR}{UpdatedAdministrativeLevel.Id}");

            return RedirectToPage();

        }


        ///Uploading excel
        ///

        // ------------------ EXCEL UPLOAD ADMINISTRATIVE LEVEL------------------
        public async Task<IActionResult> OnPostUploadExcelAdministrativeLevelsAsync()
        {
            _logger.LogInformation("Starting Upload Administrative Levels upload process at {Time}.", DateTime.UtcNow);

            // Step 1: Validate uploaded file
            if (ExcelFile == null || ExcelFile.Length == 0)
            {
                TempData["UploadErrorMessage"] = "Please select a valid Excel file before uploading.";
                _logger.LogWarning("Administrative Levels Excel upload aborted: no file selected or file is empty.");
                return RedirectToPage();
            }

            try
            {
                // Step 2: Send file to backend API for processing
                _logger.LogInformation("Posting Excel file {FileName} ({FileSize} bytes) to backend API endpoint v1/AdministrativeLevels/upload-excel.",
                    ExcelFile.FileName, ExcelFile.Length);

                var response = await _api.PostFileAsync("v1/AdministrativeLevels/upload-excel", ExcelFile);

                // Step 3: Handle success response
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Administrative Levels Excel uploaded and processed successfully by backend API.");
                    TempData["SuccessMessage"] = "Administrative Levels  Excel uploaded and processed successfully.";
                }
                else
                {
                    // Step 4: Handle backend validation or SQL errors
                    var err = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Backend API returned an error during Administrative Levels Excel upload: {ErrorMessage}", err);

                    // Detect SQL validation message for missing LookupNames
                    if (err.Contains("Missing sku", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Formatting missing sku validation message for frontend display.");

                        // Clean up raw backend error message
                        var cleaned = err
                            .Replace("Missing name(s):", "")
                            .Replace("Please create them first in  .", "")
                            .Replace("Upload failed because", "")
                            .Trim();

                        // Extract lookup names enclosed in square brackets (e.g., [LkpGender])
                        var matches = Regex.Matches(cleaned, @"\[(.*?)\]");
                        var missingList = matches.Select(m => m.Groups[1].Value).ToList();

                        // Build an HTML bullet list for display
                        var htmlList = string.Join("", missingList.Select(x => $"<li>{x}</li>"));

                        // Construct the final formatted HTML message
                        var formattedMessage = $@"
                            Upload failed because the following name  are missing. 
                            Add them in Manage Administrative Levels to proceed:
                            <ul class='mb-0'>
                                {htmlList}
                            </ul>";

                        TempData["ErrorMessage"] = formattedMessage;
                        _logger.LogWarning("Administrative Level Excel upload failed due to missing Administrative Levels names: {MissingList}", string.Join(", ", missingList));
                    }
                    else
                    {
                        // Non-lookup validation errors (generic database or API failures)
                        TempData["ErrorMessage"] = $"Upload failed: {err}";
                        _logger.LogError("Administrative Level Excel upload failed with non-specific error: {Error}", err);
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch-all for unexpected errors (e.g., connectivity or internal issues)
                TempData["ErrorMessage"] = "An unexpected error occurred while uploading the file.";
                _logger.LogError(ex, "Unexpected exception occurred during Administrative Level  Excel upload at {Time}.", DateTime.UtcNow);
            }

            // Step 5: Redirect back to the same page to display result messages
            _logger.LogInformation("Redirecting back to Manage  Administrative Level  page after upload attempt.");
            return RedirectToPage();
        }

        // ------------------ EXCEL UPLOAD  ADMINISTRATIVE LEVEL LOCATION ------------------
        public async Task<IActionResult> OnPostUploadExcelAdministrativeLevelLocationAsync()
        {
            _logger.LogInformation("Starting Upload Administrative Level Locations upload process at {Time}.", DateTime.UtcNow);

            // Step 1: Validate uploaded file
            if (ExcelFile == null || ExcelFile.Length == 0)
            {
                TempData["UploadErrorMessage"] = "Please select a valid Excel file before uploading.";
                _logger.LogWarning("Administrative Level Locations Excel upload aborted: no file selected or file is empty.");
                return RedirectToPage();
            }

            try
            {
                // Step 2: Send file to backend API for processing
                _logger.LogInformation("Posting Excel file {FileName} ({FileSize} bytes) to backend API endpoint v1/AdministrativeLevels/upload-excelAdministrativeLevelLocation.",
                    ExcelFile.FileName, ExcelFile.Length);

                var response = await _api.PostFileAsync("v1/AdministrativeLevels/upload-excelAdministrativeLevelLocation", ExcelFile);

                // Step 3: Handle success response
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Administrative Level Locations Excel uploaded and processed successfully by backend API.");
                    TempData["SuccessMessage"] = "Administrative Level Locations  Excel uploaded and processed successfully.";
                }
                else
                {
                    // Step 4: Handle backend validation or SQL errors
                    var err = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Backend API returned an error during Administrative Level Locations Excel upload: {ErrorMessage}", err);

                    // Detect SQL validation message for missing LookupNames
                    if (err.Contains("Missing Name", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Formatting missing sku validation message for frontend display.");

                        // Clean up raw backend error message
                        var cleaned = err
                            .Replace("Missing name(s):", "")
                            .Replace("Please create them first in  .", "")
                            .Replace("Upload failed because", "")
                            .Trim();

                        // Extract lookup names enclosed in square brackets (e.g., [LkpGender])
                        var matches = Regex.Matches(cleaned, @"\[(.*?)\]");
                        var missingList = matches.Select(m => m.Groups[1].Value).ToList();

                        // Build an HTML bullet list for display
                        var htmlList = string.Join("", missingList.Select(x => $"<li>{x}</li>"));

                        // Construct the final formatted HTML message
                        var formattedMessage = $@"
                            Upload failed because the following name  are missing. 
                            Add them in Manage Administrative Level Locations to proceed:
                            <ul class='mb-0'>
                                {htmlList}
                            </ul>";

                        TempData["ErrorMessage"] = formattedMessage;
                        _logger.LogWarning("Administrative Level Locations Excel upload failed due to missing Administrative Level Locationsnames: {MissingList}", string.Join(", ", missingList));
                    }
                    else
                    {
                        // Non-lookup validation errors (generic database or API failures)
                        TempData["ErrorMessage"] = $"Upload failed: {err}";
                        _logger.LogError("Administrative Level Locations Excel upload failed with non-specific error: {Error}", err);
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch-all for unexpected errors (e.g., connectivity or internal issues)
                TempData["ErrorMessage"] = "An unexpected error occurred while uploading the file.";
                _logger.LogError(ex, "Unexpected exception occurred during Administrative Level Locations  Excel upload at {Time}.", DateTime.UtcNow);
            }

            // Step 5: Redirect back to the same page to display result messages
            _logger.LogInformation("Redirecting back to Manage  Administrative Level Locations  page after upload attempt.");
            return RedirectToPage();
        }

        // ------------------ DOWNLOAD LOOKUP EXCEL ------------------
        public async Task<IActionResult> OnPostDownloadLookupExcelAsync()
        {
            _logger.LogInformation("Generating Excel file for all lookup values...");

            try
            {
                // Fetch all lookup names and values
                var lookupNames = await _api.GetAsync<List<LookupTableNameDto>>("v1/Lookups/lookups") ?? new();
                var lookupValues = await _api.GetAsync<List<LookupTableValueDto>>("v1/Lookups/values") ?? new();

                // Create a temporary Excel file in memory
                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Lookup Items");

                // Add headers
                worksheet.Cell(1, 1).Value = "Lookup ID";
                worksheet.Cell(1, 2).Value = "Lookup Name";
                worksheet.Cell(1, 3).Value = "Item ID";
                worksheet.Cell(1, 4).Value = "Item Name";
                worksheet.Cell(1, 5).Value = "Is Active";
                worksheet.Cell(1, 6).Value = "Created Date";
                worksheet.Cell(1, 7).Value = "Updated Date";

                // Populate data
                int row = 2;
                foreach (var v in lookupValues.OrderByDescending(x => x.CreatedDate ?? DateTime.MinValue))
                {
                    var lookupName = lookupNames.FirstOrDefault(l => l.Id == v.LookupId)?.LookupName ?? "N/A";
                    worksheet.Cell(row, 1).Value = v.LookupId;
                    worksheet.Cell(row, 2).Value = lookupName;
                    worksheet.Cell(row, 3).Value = v.Id;
                    worksheet.Cell(row, 4).Value = v.ItemName;
                    worksheet.Cell(row, 5).Value = v.IsActive ? "Yes" : "No";
                    worksheet.Cell(row, 6).Value = v.CreatedDate?.ToString("yyyy-MM-dd HH:mm") ?? "";
                    worksheet.Cell(row, 7).Value = v.UpdatedDate?.ToString("yyyy-MM-dd HH:mm") ?? "";
                    row++;
                }

                // Apply basic styling
                var headerRange = worksheet.Range("A1:G1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGreen;
                worksheet.Columns().AdjustToContents();

                // Save to memory stream
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                _logger.LogInformation("Excel file generated successfully with {RowCount} rows.", row - 2);

                // Return as downloadable file
                return File(stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            $"LookupItems_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel file for lookup items.");
                TempData["ErrorMessage"] = "Failed to generate Excel file. Please try again.";
                return RedirectToPage();
            }
        }
        //ende

    }
}
