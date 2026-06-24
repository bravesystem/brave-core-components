using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using static BRaVe_Management_Backend.Services.SqlClaimService;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IDistributionAssistanceService
    {
        
        Task<IEnumerable<DistributionAssistance>> GetAllDistributionAssistance(int TenantId);
         Task<DistributionAssistance?> GetDistributionAssistanceById(int id); //
         Task UpdateDistributionAssistance(DistributionAssistance data, string UserId);  //

        Task CreateDistributionAssistance(DistributionAssistanceDto data, string UserId);
        Task GetDistributionKitByDistributionAssistanceId(int DistributionId);
    }
}
