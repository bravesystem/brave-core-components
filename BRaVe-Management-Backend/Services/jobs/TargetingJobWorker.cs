
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Interfaces.jobs;
using BRaVe_Management_Backend.Models;
using BRaVe_Management_Backend.Models.es;
using DocumentFormat.OpenXml.Drawing;

namespace BRaVe_Management_Backend.Services.jobs
{
    public class TargetingJobWorker : BackgroundService
    {

        private readonly ITargetingJob _targetingJob;
        private readonly ITargetingJobQueue _queue;
        private readonly ILogger<TargetingJobWorker> _logger;

        private readonly ISearchService _searchService;
        private readonly IScoringEngine _scoringEngine;

        public TargetingJobWorker(
            ITargetingJob targetingJob,
            ITargetingJobQueue queue,
            ISearchService searchService,
            IScoringEngine scoringEngine,
            ILogger<TargetingJobWorker> logger)
        {
            _targetingJob = targetingJob;
            _queue = queue;// ?? throw new ArgumentNullException(nameof(queue));
            _searchService = searchService;
            _scoringEngine = scoringEngine;
            _logger = logger;// ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Targeting job worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    TargetingJob? job = null;

                    var jobIdFromQueue = await _queue.DequeueAsync(TimeSpan.FromSeconds(1), stoppingToken);
                    if (jobIdFromQueue.HasValue)
                    {
                        job = await _targetingJob.ClaimJobByIdAsync(jobIdFromQueue.Value, stoppingToken);
                    }

                    //job ??= await _targetingJob.ClaimNextQueuingJobAsync(stoppingToken);

                    if (job == null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        continue;
                    }

                    _logger.LogInformation("Processing targeting job {JobId}.", job.Id);

                    try
                    {
                        await ProcessTargetingJobAsync(job, stoppingToken);
                        await _targetingJob.SetCompletedAsync(job.Id, stoppingToken);
                        _logger.LogInformation("Targeting job {JobId} completed.", job.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Targeting job {JobId} failed.", job.Id);
                        await _targetingJob.SetFailedAsync(job.Id, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in targeting job worker.");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("Targeting job worker stopped.");
        }

        private async Task ProcessTargetingJobAsync(TargetingJob job, CancellationToken cancellationToken)
        {
            // TODO: Implement actual targeting logic
            //await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

            List<Household_Document>
                household_Documents = await _searchService.GetAllHouseholds(job.TenantId, job.StartPeriod, job.EndPeriod);

            if (household_Documents.Count > 0)
            {
                RuleDefinition ruleDefinition = await GetRuleDefinition(job.TargetingId);

                List<ScoredResult> scoredResults = await _scoringEngine.SearchAsync(household_Documents, ruleDefinition);

                _targetingJob.SaveTargetingResultsAsync(job.Id, job.TenantId, scoredResults);
            }


            //await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        }

        private async Task<RuleDefinition> GetRuleDefinition(int targetingId)
        {
            return await _searchService.GetRuleDefinition(targetingId);
        }
    }
}
