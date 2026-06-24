using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IClaimSessionService
    {
      
        Task<IEnumerable<ClaimSession>> GetAllClaimSessions(int tenantId);
         Task<ClaimSession?> GetClaimSessionById(int id); //
         Task UpdateClaimSession(ClaimSession data, string UserId);  //

        Task RevokeClaimSession(string UserId, int id);
        Task<Enumerator?> GetEnumeratorById(int id);
        Task CreateEnumeratorCodeBatch(EnumeratorCodeBatchDto data, string UserId);
    }
}
