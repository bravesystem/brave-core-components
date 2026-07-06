using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Threading;

namespace BRaVe_Portal.Pages.FileBasedDeduplication
{
    public class IndexModel : PageModel
    {
        //private const string ProcessUploadPath = "v1/FileBasedDeduplication/process";

        private readonly IRestApiService _api;
        private readonly ILogger<IndexModel> _logger;

        private const int RecentUploadsLimit = 10;

        public IndexModel(IRestApiService api, ILogger<IndexModel> logger)
        {
            _api = api;
            _logger = logger;
        }

        public IList<UploadedFile> RecentUploads { get; private set; } = [];


        [BindProperty]
        public IFormFile? Upload { get; set; }

        public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
        {
            try
            {
                //RecentUploads = GetMockUploads().Take(10).ToList();
                var uploads = await _api.GetAsync<List<UploadedFile>>(
                $"v1/FileBasedDeduplication/uploads?maxCount={RecentUploadsLimit}",
                cancellationToken);

                RecentUploads = uploads ?? [];
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Unexpected error during upload.");
                TempData["ErrorMessage"] = "An unexpected error occurred while uploading the file.";
                RecentUploads = [];
            }

            return Page();
            
        }

        public async Task<IActionResult> OnPostUploadAsync(CancellationToken cancellationToken)
        {
            if (Upload is null || Upload.Length == 0)
            {
                TempData["UploadError"] = "Please select a file to upload.";
                return RedirectToPage();
            }

            if (!string.Equals(Path.GetExtension(Upload.FileName), ".json", StringComparison.OrdinalIgnoreCase))
            {
                TempData["UploadError"] = "Only .json files are allowed.";
                return RedirectToPage();
            }

            var response = await _api.PostFileAsync("v1/FileBasedDeduplication/process", Upload, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<FileBasedDeduplicationProcessResult>(
                    cancellationToken: cancellationToken);

                TempData["UploadSuccess"] = !string.IsNullOrWhiteSpace(result?.Message)
                    ? result.Message
                    : $"\"{Upload.FileName}\" was uploaded successfully.";

                return RedirectToPage();
            }

            TempData["UploadError"] = await ReadErrorMessageAsync(response, cancellationToken);
            return RedirectToPage();
        }

        private static async Task<string> ReadErrorMessageAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                if (document.RootElement.TryGetProperty("error", out var errorProperty) &&
                    errorProperty.ValueKind == JsonValueKind.String)
                {
                    var error = errorProperty.GetString();
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        return error;
                    }
                }
            }
            catch (JsonException)
            {
                // Fall through to generic message.
            }

            return $"Upload failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
        }

        private static List<UploadedFile> GetMockUploads()
        {
            var now = DateTime.UtcNow;

            return
            [
                new UploadedFile
            {
                JobId = Guid.NewGuid(),
                Filename = "mission_alpha_registrations.csv",
                UploadedBy = "j.doe@iom.int",
                UploadedOn = now.AddHours(-2),
                IsProcessed = true,
                ProcessedOn = now.AddHours(-1)
            },
            new UploadedFile
            {
                JobId = Guid.NewGuid(),
                Filename = "feb_biometric_batch.xlsx",
                UploadedBy = "a.smith@iom.int",
                UploadedOn = now.AddDays(-1),
                IsProcessed = true,
                ProcessedOn = now.AddDays(-1).AddHours(3)
            },
            new UploadedFile
            {
                JobId = Guid.NewGuid(),
                Filename = "cross_site_export.csv",
                UploadedBy = "m.lee@iom.int",
                UploadedOn = now.AddDays(-2),
                IsProcessed = false
            },
            new UploadedFile
            {
                JobId = Guid.NewGuid(),
                Filename = "verification_candidates.csv",
                UploadedBy = "j.doe@iom.int",
                UploadedOn = now.AddDays(-3),
                IsProcessed = true,
                ProcessedOn = now.AddDays(-3).AddHours(5)
            },
            new UploadedFile
            {
                JobId = Guid.NewGuid(),
                Filename = "legacy_import_part2.xlsx",
                UploadedBy = "r.khan@iom.int",
                UploadedOn = now.AddDays(-4),
                IsProcessed = true,
                ProcessedOn = now.AddDays(-4).AddHours(2)
            },
            new UploadedFile
            {
                JobId = Guid.NewGuid(),
                Filename = "duplicate_review_input.csv",
                UploadedBy = "a.smith@iom.int",
                UploadedOn = now.AddDays(-5),
                IsProcessed = false
            },
            new UploadedFile
            {
                JobId = Guid.NewGuid(),
                Filename = "mission_beta_enrollments.csv",
                UploadedBy = "s.nguyen@iom.int",
                UploadedOn = now.AddDays(-6),
                IsProcessed = true,
                ProcessedOn = now.AddDays(-6).AddHours(4)
            },
            new UploadedFile
            {
                JobId = Guid.NewGuid(),
                Filename = "january_biometrics.xlsx",
                UploadedBy = "j.doe@iom.int",
                UploadedOn = now.AddDays(-8),
                IsProcessed = true,
                ProcessedOn = now.AddDays(-8).AddHours(6)
            },
            new UploadedFile
            {
                JobId = Guid.NewGuid(),
                Filename = "field_office_upload.csv",
                UploadedBy = "m.lee@iom.int",
                UploadedOn = now.AddDays(-10),
                IsProcessed = true,
                ProcessedOn = now.AddDays(-10).AddHours(1)
            },
            new UploadedFile
            {
                JobId = Guid.NewGuid(),
                Filename = "initial_pilot_batch.csv",
                UploadedBy = "r.khan@iom.int",
                UploadedOn = now.AddDays(-12),
                IsProcessed = true,
                ProcessedOn = now.AddDays(-12).AddHours(8)
            }
            ];
        }
    }

}
