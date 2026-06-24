using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using static BRaVe_Management_Backend.Services.SqlClaimService;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface ILocationService
    {
         //Task<IEnumerable<Enumerator>> AllEnumerators { get;}
        Task<IEnumerable<Location>> GetAllLocations(int TenantId);
        Task<IEnumerable<Location>> GetAllLocationbytenantId(int tenantId);

        //Task<Location?> GetLocationById(int id); //
         Task UpdateLocation(Location data, string UserId);  //

        Task CreateLocation(LocationDto data, string UserId);

        Task<string> UploadAdministrativeLevelLocationExcelAsync(IFormFile excelFile, string userId, int tenantId);

        Task<byte[]> DownloadAdministrativeLevelLocationTemplateAsync(int tenantId);

    }
}
