using BRaVe_Management_Backend.Interfaces;
using System.Threading.Channels;

namespace BRaVe_Management_Backend.Services.jobs
{
    public class DeduplicationJobQueue : IDeduplicationJobQueue
    {

        private readonly Channel<long> _channel = Channel.CreateUnbounded<long>(new UnboundedChannelOptions { SingleReader = true });

        public void Enqueue(long jobId) => _channel.Writer.TryWrite(jobId);

        public async ValueTask<long?> DequeueAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
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
