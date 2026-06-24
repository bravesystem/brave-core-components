using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Interfaces.jobs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IDeviceProvisioningMonitoringService
    {
        Task<List<DeviceProvisioningErrorGridRowDto>> GetProvisioningErrorsAsync(string? deviceId = null);
        Task DetachDeviceAsync(string deviceId, int CurrentTenantId, int OldTenantId, string reason, string approvedBy);
    }

}
