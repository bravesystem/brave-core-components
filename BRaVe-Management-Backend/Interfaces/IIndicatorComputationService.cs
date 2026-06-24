using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using System.Text.Json;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IIndicatorComputationService
    {

        Task<List<ComputedIndicatorValueDto>> ComputeAsync(
                int TenantId,
                TargetingRunDto run,
                List<Guid> entityIds);

     
    }
}
