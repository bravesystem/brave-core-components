using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using static BRaVe_Management_Backend.Services.SqlClaimService;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IDistributionCompositionService
    {

        Task<IEnumerable<DistributionComposition>> GetAllDistributionComposition(int distributionId,int TenantId);
        Task<IEnumerable<DistributionComposition>> GetDistributionCompositionsAsync( int TenantId);
        Task<IEnumerable<DistributionItemTypes>> GetDistributionItemsAsync(int tenantId);
        Task<long> CreateDistributionItemAsync(DistributionItemTypes item);
        Task UpdateDistributionItemAsync(DistributionItemTypes item);
        Task AddDistributionItemsAsync(List<DistributionItemCreateDto> dtoList);
        Task AddDistributionKitsAsync(List<DistributionKitCreateDto> kits);
        Task<List<KitTypeItem>> GetAllKitItemsAsync();
        Task<List<KitItemsDto>> GetKitItemByKitIdAsync(long kitId, int tenantId);
        Task AddKitItemsAsync( int tenantId, string createdByUserId, List<KitItemInsertDto> items);
        Task UpdateInline(int id, decimal measure, int uoM);
        Task UpdateInlineItemDistribution(int id, DistributionItemCreateDto dto);
        Task UpdateInlineKitDistribution(DistributionKitCreateDto dto);
    }


    
}
