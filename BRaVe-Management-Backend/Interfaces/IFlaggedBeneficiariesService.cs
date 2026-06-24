
using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IFlaggedBeneficiariesService
    {
        Task AutoPartition(int tenantId, AutoPartitionParams autoPartition);
        Task<List<FlaggedBeneficiary>> GetAll(int tenantId);
        Task<string> Group(int tenantId, List<string> group);
        Task InsertFlaggedBeneficiaries(CreateFlaggedBeneficiaryRequest request);
    }
}
