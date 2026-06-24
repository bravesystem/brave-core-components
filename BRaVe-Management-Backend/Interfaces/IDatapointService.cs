using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IDatapointService
    {
      
        Task<IEnumerable<DataPointDto>> GetAllDataPoints();
        Task<DataPointDto> GetDataPointById(int id);
        Task CreateDataPoint(DataPointDto newDataPoint, string userId);
        Task UpdateDataPoint(DataPointDto updatedDataPoint);
        Task<List<DataPointDto>> GetDataPointsByTenantAsync(int tenantId, string languageCode);
        Task DeleteDatapoint(string userId, int id, int tenantId);

        // transaltions
        Task<DatapointTranslationDto?> GetTranslationByDataPointIdAsync(int dataPointId);

        Task<IEnumerable<DatapointTranslationDto>> GetAllTranslationsAsync(int dataPointId);

        Task AddOrUpdateTranslationAsync(DatapointTranslationDto dto, string userId);

        Task DeleteTranslationAsync(int dataPointId, string languageCode);

        Task<List<DataPointDto>> GetDataPointsForActivityByTenantAsync(int tenantId, string languageCode);
        
    }
}
