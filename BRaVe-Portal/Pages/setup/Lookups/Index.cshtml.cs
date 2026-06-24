using BRaVe_Management_Backend.DTOs;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using System.Text.Json.Serialization;
using System.Text.Json;
using BRaVe_Portal.Extensions;

namespace BRaVe_Portal.Pages.Setup.Lookups
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IRestApiService _api;
        //private readonly IAppCache _cache;

        public IndexModel(ILogger<IndexModel> logger, IRestApiService api)
        {
            _logger = logger;
            _api = api;
            //_cache = cache;
        }
        public IFormFile ExcelFile { get; set; } = null;

        [BindProperty(SupportsGet = true)]
        public List<LookupTableNameDto> LookupNames { get; private set; } = new();

        [BindProperty]
        public LookupTableNameDto NewLookup { get; set; } = new();

        [BindProperty]
        public LookupTableNameDto UpdatedLookup { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public List<LookupTableValueDto> LookupValues { get; set; } = new();

        [BindProperty]
        public LookupTableValueDto NewLookupValue { get; set; } = new();

        [BindProperty]
        public LookupTableValueDto UpdatedLookupValue { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        public string? DisplaySearchTerm { get; private set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalPages { get; set; }


        // ------------------ LOAD LOOKUPS ------------------
        public async Task<IActionResult> OnGetAsync()
        {
            if (Request.ContainsPathProbeInQuery())
            {
                _logger.LogError("Blocked suspicious query payload on Lookups GET.");
                return BadRequest("Invalid request.");
            }

            if (!Request.TryGetOptionalPositiveIntFromQuery(_logger,"CurrentPage", out var validatedCurrentPage) ||
                !Request.TryGetOptionalPositiveIntFromQuery(_logger,"PageSize", out var validatedPageSize) ||
                !Request.TryGetSafeOptionalTextFromQuery(_logger, "SearchTerm", out var validatedSearchTerm, 256))
            {
                _logger.LogError("Blocked suspicious query payload on Lookups GET.");
                return BadRequest("Invalid request.");
            }

            if (validatedCurrentPage.HasValue)
            {
                CurrentPage = validatedCurrentPage.Value;
            }

            if (validatedPageSize.HasValue)
            {
                PageSize = Math.Min(validatedPageSize.Value, 200);
            }

            SearchTerm = validatedSearchTerm;
            DisplaySearchTerm = SearchTerm;

            _logger.LogInformation("OnGetAsync called to load lookup values and names. HasSearch={HasSearch}",
              !string.IsNullOrWhiteSpace(SearchTerm));

            try
            {
                // Fetch lookup names (offcanvas)
                LookupNames = await _api.GetAsync<List<LookupTableNameDto>>("v1/Lookups/lookups") ?? new();

                // Fetch lookup values (main table)
                var allValues = await _api.GetAsync<List<LookupTableValueDto>>("v1/Lookups/values") ?? new();

                // Pagination + search
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    string search = SearchTerm.Trim().ToLower();
                    allValues = allValues
                        .Where(v => v.ItemName.ToLower().Contains(search)
                                 || LookupNames.FirstOrDefault(l => l.Id == v.LookupId)?.LookupName.ToLower().Contains(search) == true)
                        .ToList();
                }

                int totalItems = allValues.Count;
                TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

                // Ensure current page is within range
                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

                // Order by CreatedDate descending (latest first)
                LookupValues = allValues
                    .OrderByDescending(v => v.CreatedDate ?? DateTime.MinValue)
                    .ThenByDescending(v => v.UpdatedDate ?? DateTime.MinValue)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();


                _logger.LogInformation("Loaded {Count} lookup values (page {Page}/{Total}).", LookupValues.Count, PageSize, TotalPages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching lookup values.");
                LookupValues = new List<LookupTableValueDto>();

                TempData["ErrorMessage"] = "Unable to load lookup values.";
            }

            return Page();
        }


        // ------------------ CREATE LOOKUP ------------------

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddLookupAsync()
        {

            //if (Request.Query.Count > 0 && Request.Query.Any(q => StringHelper.IsPotentialPathProbe(q.Value.ToString())))
            if (Request.ContainsPathProbeInQuery() || 
                await Request.ContainsPathProbeInFormAsync() ||
                StringHelper.IsPotentialPathProbe(SearchTerm))
            {
                _logger.LogError("Blocked suspicious query payload on AddLookup POST.");
                return BadRequest("Invalid request.");
            }

            ModelState.Remove(nameof(ExcelFile));
            ModelState.Remove(nameof(SearchTerm));

            _logger.LogInformation("OnPostAddLookupAsync called to add new lookup: {LookupName}", NewLookup.LookupName);

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Could not save lookup. Please try again.");
                _logger.LogWarning("ModelState invalid when adding lookup: {LookupName}", NewLookup.LookupName);
                LookupNames = await _api.GetAsync<List<LookupTableNameDto>>("v1/Lookups/lookups") ?? new();
                TempData["KeepCanvasOpen"] = true;
                TempData["LookupErrorMessage"] = "Failed to add lookup. Please try again";

                return RedirectToPage();
            }

            try
            {
                await _api.PostJsonAsync<LookupTableNameDto, object>("v1/Lookups/lookups", NewLookup);
                _logger.LogInformation("Lookup added and cache cleared: {LookupName}", NewLookup.LookupName);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding lookup: {LookupName}", NewLookup.LookupName);
                ModelState.AddModelError(string.Empty, "Could not save lookup. Please try again.");
                TempData["LookupErrorMessage"] = "Could not save lookup. Please try again.";
            }

            TempData["KeepCanvasOpen"] = true;
            TempData["LookupSuccessMessage"] = "Look up added successfully";

            return RedirectToPage();
        }

        // ------------------ UPDATE LOOKUP ------------------

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditLookupAsync()
        {
            _logger.LogInformation("OnPostEditLookupAsync called to edit lookup: {LookupId}", UpdatedLookup.Id);

            if (Request.Query.Count > 0 && Request.Query.Any(q => StringHelper.IsPotentialPathProbe(q.Value.ToString())))
            {
                _logger.LogWarning("Blocked suspicious query payload on EditLookup POST.");
                TempData["ErrorMessage"] = "Invalid request.";
                //return RedirectToPage("./Index");
                return RedirectToPage();
            }

            ModelState.Remove(nameof(ExcelFile));
            ModelState.Remove(nameof(SearchTerm));

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Could not update lookup. Please try again.");
                _logger.LogWarning("ModelState invalid when editing lookup: {LookupId}", UpdatedLookup.Id);
                LookupNames = await _api.GetAsync<List<LookupTableNameDto>>("v1/Lookups/lookups") ?? new();
                TempData["KeepCanvasOpen"] = true;
                TempData["LookupErrorMessage"] = "Failed to edit lookup. Please try again";

                return RedirectToPage();
            }

            try
            {
                await _api.PutJsonAsync<LookupTableNameDto, object>($"v1/Lookups/lookups/{UpdatedLookup.Id}", UpdatedLookup);
                _logger.LogInformation("Lookup updated and cache cleared: {LookupId}", UpdatedLookup.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing lookup: {LookupId}", UpdatedLookup.Id);
                ModelState.AddModelError(string.Empty, "Could not update lookup. Please try again.");
            }

            TempData["KeepCanvasOpen"] = true;
            TempData["LookupSuccessMessage"] = "Look up edited successfully";

            return RedirectToPage();
        }

        // ------------------ DELETE LOOKUP ------------------

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteLookupAsync(int id)
        {
            _logger.LogInformation("OnPostDeleteLookupAsync called to delete lookup: {LookupId}", id);

            if (Request.ContainsPathProbeInQuery() ||
               await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on DeleteLookup POST.");
                return BadRequest("Invalid request.");
            }

            try
            {
                await _api.DeleteAsync($"v1/Lookups/lookups/{id}");
                _logger.LogInformation("Lookup deleted and cache cleared: {LookupId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lookup: {LookupId}", id);
                ModelState.AddModelError(string.Empty, "Could not delete lookup. Please try again.");
            }

            TempData["KeepCanvasOpen"] = true;
            TempData["LookupSuccessMessage"] = "Look up deleted successfully";

            return RedirectToPage();
        }


        // ------------------ ADD LOOKUP VALUE ------------------
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddLookupValueAsync()
        {
            
            if (Request.ContainsPathProbeInQuery() ||
               await Request.ContainsPathProbeInFormAsync() ||
               StringHelper.IsPotentialPathProbe(SearchTerm))
            {
                _logger.LogError("Blocked suspicious query payload on AddLookupValue POST.");
                return BadRequest("Invalid request.");
            }

            _logger.LogInformation("Adding new lookup value: {ItemName}", NewLookupValue.ItemName);


            ModelState.Remove(nameof(ExcelFile));
            ModelState.Remove(nameof(SearchTerm));
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid data. Please check your input.";
                return RedirectToPage();
            }

            try
            {
                await _api.PostJsonAsync<LookupTableValueDto, object>("v1/Lookups/values", NewLookupValue);
                TempData["SuccessMessage"] = "Lookup value added successfully.";
                _logger.LogInformation("Lookup value added: {ItemName}", NewLookupValue.ItemName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding lookup value.");
                TempData["ErrorMessage"] = "Failed to add lookup value.";
            }

            return RedirectToPage();
        }


        // ------------------ EDIT LOOKUP VALUE ------------------
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditLookupValueAsync()
        {
            
            if (Request.ContainsPathProbeInQuery() ||
               await Request.ContainsPathProbeInFormAsync() ||
               StringHelper.IsPotentialPathProbe(SearchTerm))
            {
                _logger.LogWarning("Blocked suspicious query payload on EditLookupValue POST.");
                return BadRequest("Invalid request.");
            }

            _logger.LogInformation("Editing lookup value ID: {Id}", UpdatedLookupValue.Id);

            ModelState.Remove(nameof(ExcelFile));
            ModelState.Remove(nameof(SearchTerm));

            if (!ModelState.IsValid)
            {
                TempData["LookupErrorMessage"] = "Invalid data. Please check your input.";
                return RedirectToPage();
            }


            try
            {
                await _api.PutJsonAsync<LookupTableValueDto, object>(
                    $"v1/Lookups/values/{UpdatedLookupValue.Id}", UpdatedLookupValue);

                TempData["LookupSuccessMessage"] = "Lookup value updated successfully.";
                _logger.LogInformation("Lookup value updated successfully: {Id}", UpdatedLookupValue.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lookup value.");
                TempData["LookupErrorMessage"] = "Failed to update lookup value.";
            }

            return RedirectToPage();
        }


        // ------------------ EXCEL UPLOAD (V2) ------------------

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostUploadLookupExcelV2Async()
        {
            _logger.LogInformation("Starting lookup Excel V2 upload process at {Time}.", DateTime.UtcNow);

            if (Request.ContainsPathProbeInQuery() ||
               await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on UploadLookupExcelV2 POST.");
                return BadRequest("Invalid request.");
            }

            // Step 1: Validate uploaded file
            if (ExcelFile == null || ExcelFile.Length == 0)
            {
                TempData["UploadErrorMessage"] = "Please select a valid Excel file before uploading.";
                _logger.LogWarning("Lookup Excel upload aborted: no file selected or file is empty.");
                return RedirectToPage();
            }

            try
            {
                // Step 2: Send file to backend API (NEW ENDPOINT)
                _logger.LogInformation("Posting Excel file {FileName} ({FileSize} bytes) to backend API endpoint v1/Lookups/upload-excel-v2.",
                    ExcelFile.FileName, ExcelFile.Length);

                var response = await _api.PostFileAsync("v1/Lookups/upload-excel-v2", ExcelFile);

                // Step 3: Handle success response
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Lookup Excel V2 uploaded and processed successfully.");
                    TempData["SuccessMessage"] = "Lookup Excel uploaded and processed successfully.";
                }
                else
                {

                    var err = JsonSerializer.Deserialize<ApiResponseBasic>(await response.Content.ReadAsStringAsync());


                    _logger.LogWarning("Backend API returned an error during lookup Excel upload: {ErrorMessage}", err.errorMessage);

                    //  Handle LookupNames validation errors (new cases)
                    if (err.errorCode == 60003)
                    {
                        TempData["ErrorMessage"] = err.errorMessage;
                    }
                    //  Handle missing lookup names for values
                    else if (err.errorCode == 60005)
                    {
                        var matches = Regex.Matches(err.errorMessage, @"\[(.*?)\]");
                        var missingList = matches.Select(m => m.Groups[1].Value).ToList();

                        var htmlList = string.Join("", missingList.Select(x => $"<li>{x}</li>"));

                        TempData["ErrorMessage"] = $@"
                    Upload failed because the following Lookup Names are missing or invalid:
                    <ul class='mb-0'>
                        {htmlList}
                    </ul>";
                    }
                    //  Duplicate names error
                    else if (err.errorCode == 60002)
                    {
                        TempData["ErrorMessage"] = "Upload failed: Duplicate LookupNames found in the LookupNames sheet.";
                    }
                    else
                    {
                        // Generic fallback
                        TempData["ErrorMessage"] = $"Upload failed: {err.errorMessage}";
                        _logger.LogError("Lookup Excel upload failed with non-specific error: {Error}", err.errorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred while uploading the file.";
                _logger.LogError(ex, "Unexpected exception occurred during lookup Excel upload at {Time}.", DateTime.UtcNow);
            }

            // Step 5: Redirect back to page
            _logger.LogInformation("Redirecting back to Manage Lookups page after upload attempt.");
            return RedirectToPage();
        }


        // ------------------ DOWNLOAD LOOKUP EXCEL ------------------
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDownloadLookupExcelAsync()
        {
            _logger.LogInformation("Generating Excel file for all lookup values...");

            if (Request.ContainsPathProbeInQuery() ||
                await Request.ContainsPathProbeInFormAsync() )
            {
                _logger.LogError("Blocked suspicious query payload on DownloadLookupExcel POST.");
                return BadRequest("Invalid request.");
            }

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

    }
}
