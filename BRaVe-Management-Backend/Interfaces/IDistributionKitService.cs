using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using static BRaVe_Management_Backend.Services.SqlClaimService;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IDistributionKitService
    {
        
        Task<IEnumerable<DistributionKit>> GetAllDistributionKit( int TenantId);

        Task<Consent?> GetDistributionKitById(int id); //
         Task UpdateDistributionKit(DistributionKit data, string UserId);  //

        Task CreateDistributionKit(DistributionKitDto data, string UserId);
        Task GetDistributionKitByDistributionKitId(int DistributionKitId);
        Task<IEnumerable<KitType>> GetAllKitTypes(int TenantId);
        Task<long> CreateKitAsync(KitType kit);
        Task UpdateKitAsync(KitType kit);
    }
}
