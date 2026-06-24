using BRaVe_Management_Backend.Interfaces;
using System.Threading.Channels;

namespace BRaVe_Management_Backend.Services
{
    public class TargetingJobQueue : ITargetingJobQueue
    {
        private readonly Channel<int> _channel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions { SingleReader = true });

        public void Enqueue(int jobId) => _channel.Writer.TryWrite(jobId);

        public async ValueTask<int?> DequeueAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);
            try
            {
                return await _channel.Reader.ReadAsync(cts.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested == false)
            {
                return null;
            }
        }
    }
}
