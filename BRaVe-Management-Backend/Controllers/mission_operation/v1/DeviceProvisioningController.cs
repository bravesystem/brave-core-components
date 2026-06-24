using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/v1/[controller]")]
[ApiController]
[Authorize]
public class DeviceProvisioningController : ControllerBase
{
    private readonly IDeviceProvisioningMonitoringService _service;
    private readonly ILogger<DeviceProvisioningController> _logger;

    public DeviceProvisioningController(
        IDeviceProvisioningMonitoringService service,
        ILogger<DeviceProvisioningController> logger)
    {
        _service = service;
        _logger = logger;
    }


    [HttpGet("errors")]
    public async Task<IActionResult> GetErrors([FromQuery] string? deviceId = null)
    {
        var data = await _service.GetProvisioningErrorsAsync(deviceId);
        return Ok(data);
    }

    [HttpPost("detach")]
    public async Task<IActionResult> DetachDevice([FromBody] DetachDeviceRequestDto request)
    {
        await _service.DetachDeviceAsync(
            request.DeviceId,
            request.CurrentTenantId,
            request.OldTenantId,
            request.DetachReason,
            User.Identity?.Name ?? "System");

        return Ok();
    }
}