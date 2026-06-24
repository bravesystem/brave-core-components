namespace BRaVe_Management_Backend.Interfaces
{
    public interface IProcessStagingJobQueue
    {
        void Enqueue(Guid jobId);
        ValueTask<Guid?> DequeueAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
    }
}
