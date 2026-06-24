using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IBeneficiaryEnrollmentsService
    {
        Task<IEnumerable<DistributionEnrollmentDto>> GetBeneficiaryEnrollmentsAsync(int tenantId, int distributionId);

            Task<IEnumerable<DistributionEnrollmentDto>> GetBeneficiaryAllEnrollmentsAsync(int tenantId);

        Task<DistributionEnrollmentDto?> GetEnrollmentByIdsAsync(
                    int tenantId,
                    int distributionId,
                    int individualId,
                    string householdId);

    }

}
