using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface ILookupService
    {
        Task<CoreLookups> GetCoreLookups(string lang=null);

        /// Uploads an Excel file containing lookup values and updates the database via a stored procedure.

        Task<bool> UploadLookupExcelV2Async(IFormFile excelFile, string userId, int tenantId);

        // New CRUD operations for tbl_CustomLookupTableNames
        Task<IEnumerable<LookupTableNameDto>> GetAllLookupNamesAsync(int tenantId);
        Task CreateLookupNameAsync(LookupTableNameDto dto, string userId, int tenantId);
        Task UpdateLookupNameAsync(LookupTableNameDto dto, string userId);
        Task DeleteLookupNameAsync(int id);

        // CRUD operations for tbl_CustomLookupTableValues
        Task<IEnumerable<LookupTableValueDto>> GetAllLookupValuesAsync(int tenantId);
        Task CreateLookupValueAsync(LookupTableValueDto dto, string userId, int tenantId);
        Task UpdateLookupValueAsync(LookupTableValueDto dto, string userId);
        Task DeleteLookupValueAsync(int id);

        Task<List<LookupTableValueDto>> GetLookupValuesByLookupIdAsync(int lookupId, int tenantId);

    }
}
