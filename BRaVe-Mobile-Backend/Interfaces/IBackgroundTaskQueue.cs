namespace BRaVe_Mobile_Backend.Interfaces
{
    public interface IBackgroundTaskQueue
    {
        void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);
        ValueTask<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
    }
}
