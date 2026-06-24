namespace BRaVe_Management_Backend.Interfaces
{
    public interface IDataLoaderService
    {
        Task LoadAsync(Guid jobId);
    }
}
