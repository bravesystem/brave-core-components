using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Interfaces.jobs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IMonitoringService
    {
        Task<List<FailedJobGridRowDto>> GetFailedBatchesAsync(int? TenatId);
        Task HandleAsync(Guid jobId);
    }

}
