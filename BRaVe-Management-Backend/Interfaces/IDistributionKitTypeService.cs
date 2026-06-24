using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using static BRaVe_Management_Backend.Services.SqlClaimService;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IDistributionKitTypeService
    {

        Task<IEnumerable<KitType>> GetAllKitTypes(int TenantId);
        Task<long> CreateKitAsync(KitType kit);
        Task UpdateKitAsync(KitType kit);

        Task<IEnumerable<KitType>> GetAllKitTypesWithItems(int TenantId);
    }
}
