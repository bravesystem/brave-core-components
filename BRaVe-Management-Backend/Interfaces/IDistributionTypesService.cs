using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using static BRaVe_Management_Backend.Services.SqlClaimService;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IDistributionTypesService
    {
        
   
        Task<IEnumerable<DistributionTypes>> GetAllDistributionTypes(int TenantId);
   
        Task<int?> GetAllDistributionKitItemss(int tenantId);
        Task<int?> GetAllDistributionItems(int tenantId);
     
        Task<DistributionTypes?> GetDistributionTypesById(int id); //

        Task UpdateDistributionType(int id, DistributionTypes dto);
        Task AddDistributionType(DistributionTypes dto);
    }


    
}
