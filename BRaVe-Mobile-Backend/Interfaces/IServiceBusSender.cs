namespace BRaVe_Mobile_Backend.Interfaces
{
    public interface IServiceBusSender
    {
        Task SendMessageAsync(string jobId);
    }
}
