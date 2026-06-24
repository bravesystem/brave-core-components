namespace BRaVe_Management_Backend.Interfaces
{
    public interface IServiceBusSender
    {
        Task SendMessageAsync(string jobId);
    }
}
