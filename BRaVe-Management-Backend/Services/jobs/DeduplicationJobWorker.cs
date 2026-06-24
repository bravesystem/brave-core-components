
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Interfaces.jobs;
using BRaVe_Management_Backend.Models;
using BRaVe_Management_Backend.Models.es;

namespace BRaVe_Management_Backend.Services.jobs
{
    public class DeduplicationJobWorker : BackgroundService
    {
        //protected override Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    throw new NotImplementedException();
        //}

        private readonly IDeduplicationJobService _deduplicationJobService;

        private readonly IDuplicateRulesetService _rulesetService;
        private readonly IDeduplicationJobQueue _queue;
        private readonly ILogger<DeduplicationJobWorker> _logger;

        private readonly ISearchService _searchService;
        private readonly IDuplicateScoringEngine _scoringEngine;

        public DeduplicationJobWorker(
            IDeduplicationJobService deduplicationJobService,
            IDuplicateRulesetService rulesetService,
            IDeduplicationJobQueue queue,
            ISearchService searchService,
            IDuplicateScoringEngine scoringEngine,
            ILogger<DeduplicationJobWorker> logger)
        {
            _deduplicationJobService = deduplicationJobService;
            _rulesetService = rulesetService;
            _queue = queue;
            _searchService = searchService;
            _scoringEngine = scoringEngine;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Deduplication job worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    DeduplicationJobDto? job = null;

                    var jobIdFromQueue = await _queue.DequeueAsync(TimeSpan.FromSeconds(1), stoppingToken);
                    if (jobIdFromQueue.HasValue)
                    {
                        job = await _deduplicationJobService.GetJobByIdAsync(jobIdFromQueue.Value, stoppingToken);
                    }

                    job ??= await _deduplicationJobService.ClaimNextQueuingJobAsync(stoppingToken);

                    if (job == null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        continue;
                    }

                    _logger.LogInformation("Processing deduplication job {JobId}.", job.JobId);

                    try
                    {
                        await ProcessDeduplicationJobAsync(job, stoppingToken);
                        await _deduplicationJobService.SetCompletedAsync(job.JobId, stoppingToken);
                        _logger.LogInformation("Deduplication job {JobId} completed.", job.JobId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Deduplication job {JobId} failed.", job.JobId);
                        await _deduplicationJobService.SetFailedAsync(job.JobId, stoppingToken);
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

        private async Task ProcessDeduplicationJobAsync(DeduplicationJobDto job, CancellationToken cancellationToken)
        {

            List<Household_Document>
                household_Documents = await _searchService.GetAllHouseholds(job.TenantId, job.StartPeriod, job.EndPeriod);

            if (household_Documents.Count > 0)
            {
                DuplicateCriteriaDefinition rule = await GetDeduplicationRule(job.TenantId, job.RuleId);

                try
                {
                    DuplicateMatchResultsViewModel matchResults = await
                           _scoringEngine.FindDuplicates(household_Documents, rule);

                    _deduplicationJobService.SaveDeduplicationResultsAsync(job.JobId, job.TenantId, matchResults.Records);
            
                }
                catch(Exception e)
                {

                }
                
            }
        }

        private async Task<DuplicateCriteriaDefinition> GetDeduplicationRule(int TenantId, int ruleId)
        {
            return await _rulesetService.GetCriteriaDefinitionAsync(TenantId, ruleId);
        }
    }
}
