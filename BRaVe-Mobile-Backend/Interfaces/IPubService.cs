namespace BRaVe_Mobile_Backend.Interfaces
{
    public interface IPubService
    {
        Task<byte[]> getClientPublickey(string DeviceId);
    }
}
