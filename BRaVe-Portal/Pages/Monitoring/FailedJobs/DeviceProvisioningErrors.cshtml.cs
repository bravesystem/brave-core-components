using BRaVe_Management_Backend.DTOs;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;

namespace BRaVe_Portal.Pages.Monitoring.FailedJobs
{
    public class DeviceProvisioningErrorsModel : PageModel
    {
        private readonly ILogger<DeviceProvisioningErrorsModel> _logger;
        private readonly IRestApiService _api;

        public DeviceProvisioningErrorsModel(
            ILogger<DeviceProvisioningErrorsModel> logger,
            IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        public List<DeviceProvisioningErrorGridRowDto> Devices { get; set; } = new();
        [BindProperty]
        public string DetachDeviceId { get; set; } = string.Empty;

        [BindProperty]
        public string OldTenantName { get; set; } = string.Empty;

        [BindProperty]
        public int OldTenantId { get; set; }
        [BindProperty]
        public int CurrentTenantId { get; set; }

        [BindProperty]
        public string DetachReason { get; set; } = string.Empty;

        public async Task OnGetAsync(string? deviceId = null)
        {
            Devices = await _api
                .GetAsync<List<DeviceProvisioningErrorGridRowDto>>(
                    $"v1/DeviceProvisioning/errors{(string.IsNullOrEmpty(deviceId) ? "" : $"?deviceId={deviceId}")}"
                ) ?? new();
        }
        public async Task<IActionResult> OnPostDetachAsync()
        {
            if (string.IsNullOrWhiteSpace(DetachReason))
            {
                TempData["ErrorMessage"] = "Reason must be provided.";
                return RedirectToPage();
            }

            try
            {
                await _api.PostJsonAsync<object, object>(
                    "v1/DeviceProvisioning/detach",
                    new
                    {
                        DeviceId = DetachDeviceId,
                        CurrentTenantId,
                        OldTenantId,
                        DetachReason
                    });

                TempData["SuccessMessage"] = "Device detached successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detach failed");
                TempData["ErrorMessage"] = "Detach failed";
            }

            return RedirectToPage();
        }
    }
}