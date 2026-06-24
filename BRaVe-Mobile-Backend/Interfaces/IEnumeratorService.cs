using BRaVe_Mobile_Backend.Models;

namespace BRaVe_Mobile_Backend.Interfaces
{
    public interface IEnumeratorService
    {
        //Task<bool> IsDefaultPin(string enumeratorCode);
        Task<List<Enumerator>> GetAll(string DeviceId, int TenantId,string? RequestIp  , int Flag);
        Task<List<Enumerator>> SetPin(SetPinRequest data);
    }
}
