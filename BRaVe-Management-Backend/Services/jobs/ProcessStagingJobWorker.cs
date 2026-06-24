
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Interfaces.jobs;

namespace BRaVe_Management_Backend.Services.jobs
{
    public class ProcessStagingJobWorker : BackgroundService
    {
        private readonly IProcessStagingJobQueue _queue;
        private readonly IDataLoaderService _loader;
        private readonly ILogger<ProcessStagingJobWorker> _logger;


        public ProcessStagingJobWorker(
            IProcessStagingJobQueue queue,
            IDataLoaderService loader,
            ILogger<ProcessStagingJobWorker> logger)
        {
            _queue = queue;
            _loader = loader;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            _logger.LogInformation("Manual Reprocessing – Mobile Sync Staging Job worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    
                    var jobIdFromQueue = await _queue.DequeueAsync(TimeSpan.FromSeconds(1), stoppingToken);

                    if (jobIdFromQueue.HasValue)
                    {
                        _logger.LogInformation("Manual Reprocessing – Mobile Sync Staging Job {JobId}.", jobIdFromQueue.Value.ToString());

                        _loader.LoadAsync(jobIdFromQueue.Value);

                        _logger.LogInformation("Manual Reprocessing – Mobile Sync Staging Job {JobId} completed.", jobIdFromQueue.Value.ToString());
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        continue;
                    }

                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Manual Reprocessing – Mobile Sync Staging Job failed.");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("Manual Reprocessing – Mobile Sync Staging Job worker stopped.");
        }
    }
}
