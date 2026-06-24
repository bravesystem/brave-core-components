using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IEnumeratorService
    {
        Task<IEnumerable<EnumeratorCodeBatch>> GetAllCodeBatches(int TenantId);
        //Task<IEnumerable<Enumerator>> AllEnumerators { get;}
        Task<IEnumerable<Enumerator>> GetAllEnumerators(int TenantId);
        //Task<IEnumerable<LookupItemDto>> GetAvailableEnumeratorCodes();//
        Task<Enumerator?> GetEnumeratorById(int id); //
        Task CreateEnumeratorCodeBatch(EnumeratorCodeBatchDto data, string UserId); 
        Task UpdateEnumerator(Enumerator data, string UserId);  //

        Task CreateEnumerator(EnumeratorDto data, string UserId);

        Task ResetEnumeratorPin(int EnumeratorId, string UserId);

        Task<IEnumerable<Enumerator>> GetAllEnumeratorCode(int TenantId);


     }
}
