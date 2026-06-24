using BRaVe_Management_Backend.Interfaces;
using System.Threading.Channels;

namespace BRaVe_Management_Backend.Services
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<Func<CancellationToken, Task>> _queue;

        public BackgroundTaskQueue(int capacity = 100)
        {
            _queue = Channel.CreateBounded<Func<CancellationToken, Task>>(capacity);
        }

        public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
        {
            if (workItem == null) throw new ArgumentNullException(nameof(workItem));

            if (!_queue.Writer.TryWrite(workItem))
            {
                // Option: throw, log, or drop
                throw new InvalidOperationException("Background task queue is full.");
            }
        }

        public async ValueTask<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            var workItem = await _queue.Reader.ReadAsync(cancellationToken);
            return workItem;
        }
    }
}
