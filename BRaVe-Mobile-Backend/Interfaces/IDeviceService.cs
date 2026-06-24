using BRaVe_Mobile_Backend.Models;

namespace BRaVe_Mobile_Backend.Interfaces
{
    public interface IDeviceService
    {
        Task<DeviceProfile> GetByIdAsync(string deviceId);
    }
}
