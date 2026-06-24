using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using static BRaVe_Management_Backend.Services.SqlClaimService;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IAdministrativeLevelService
    {
         //Task<IEnumerable<Enumerator>> AllEnumerators { get;}
        Task<IEnumerable<AdministrativeLevel>> GetAllAdministrativeLevel(int TenantId);
        Task<AdministrativeLevel?> GetAdministrativeLevelById(int id); //
        Task UpdateAdministrativeLevel(AdministrativeLevel data, string UserId); 

        Task CreateAdministrativeLevel(AdministrativeLevelDto data, string UserId);
        Task<bool> UploadAdministrativeLevelExcelAsync(IFormFile excelFile, string userId, int tenantId);
        Task<bool> UploadAdministrativeLevelLocationExcelAsync(IFormFile excelFile, string userId, int tenantId);


    }
}
