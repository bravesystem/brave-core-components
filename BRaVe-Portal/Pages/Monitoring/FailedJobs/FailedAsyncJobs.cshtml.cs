using BRaVe_Management_Backend.DTOs;
using BRaVe_Portal.Extensions;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using System.Text.Json;

namespace BRaVe_Portal.Pages.Monitoring.FailedJobs
{
    public class FailedAsyncJobsModel : PageModel
    {
        private readonly ILogger<FailedAsyncJobsModel> _logger;
        private readonly IRestApiService _api;

        public FailedAsyncJobsModel(ILogger<FailedAsyncJobsModel> logger, IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }




        public List<FailedJobGridRowDto> FailedJobs { get; set; } = new();

        // =========================
        // GET
        // =========================
        public async Task OnGetAsync()
        {
            try
            {
                var tenantId = User.Tenant();

                FailedJobs = await _api.GetAsync<List<FailedJobGridRowDto>>(
                    $"v1/Monitoring?tenantId={tenantId}")
                    ?? new List<FailedJobGridRowDto>();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Jobs");
                TempData["ErrorMessage"] = "Unable to load Jobs.";
            }
        } 

        public async Task<IActionResult> OnPostRetryAsync(Guid jobId)
        {
            try
            {
                _logger.LogInformation("Retrying failed job {JobId}", jobId);

                await _api.PostJsonAsync<object>($"v1/Monitoring/{jobId}/retry");

                // add delay here for 3 seconds
                await Task.Delay(TimeSpan.FromSeconds(5));
                TempData["SuccessMessage"] = $"Job {jobId} ran successfully!";

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running job {JobId}", jobId);
                TempData["ErrorMessage"] = "Failed to run job.";
            }

            return RedirectToPage();
        }

    }
}
