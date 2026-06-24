namespace BRaVe_Management_Backend.Interfaces
{
    public interface IDeduplicationJobQueue
    {
        void Enqueue(long jobId);
        ValueTask<long?> DequeueAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
    }
}
