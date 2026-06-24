namespace BRaVe_Management_Backend.Interfaces
{
    public interface ITargetingJobQueue
    {
        void Enqueue(int jobId);
        ValueTask<int?> DequeueAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
    }
}
