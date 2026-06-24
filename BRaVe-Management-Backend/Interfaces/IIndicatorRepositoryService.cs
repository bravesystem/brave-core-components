using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using System.Text.Json;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IIndicatorRepositoryService
    {
        Task<List<IndicatorDto>> GetActiveIndicatorsAsync(int TenantId);
        Task<IndicatorDto?> GetByCodeAsync(string code);
        Task<List<CompositeIndicatorDto>> GetCompositeIndicatorsAsync(int TenantId);
        Task<List<IndicatorDependencyDto>> GetDependenciesAsync();
        Task<IndicatorDto> SaveCompositeAsync(int TenantId, string CreatedBy, IndicatorDto dto);

        // Add this for update
        Task<IndicatorDto?> UpdateAsync(int id, int tenantId, string updatedBy, IndicatorDto dto);

        Task<IndicatorDto?> GetByIdAsync(int id); // Needed by controller
   
    }

}
