using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Interfaces.jobs;
using BRaVe_Management_Backend.Models;
using BRaVe_Management_Backend.Models.es;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Data.SqlClient;
using System.Globalization;

namespace BRaVe_Management_Backend.Controllers.search_analytics.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class DeduplicationEngineController : ControllerBase
    {


        private readonly ISearchService _searchEngine;
        private readonly IDuplicateScoringEngine _scoringEngine;
        private readonly IDuplicateRulesetService _rulesetService;
        private readonly IDeduplicationJobService _deduplicationJob;
        private readonly IDeduplicationJobQueue _queue;
        private readonly IAppCache _cache;
        private readonly ILogger<DeduplicationEngineController> _logger;

        public static int DURATION_IN_MIN = 10;

        public DeduplicationEngineController(
            IDuplicateScoringEngine scoringEngine,
            IDuplicateRulesetService rulesetService,
            IDeduplicationJobService deduplicationJob,
              ISearchService searchService,
              IDeduplicationJobQueue queue,
            IAppCache cache,
            ILogger<DeduplicationEngineController> logger)
        {
            _scoringEngine = scoringEngine;
            _deduplicationJob = deduplicationJob;
            _rulesetService = rulesetService;
            _searchEngine = searchService;
            _queue = queue;
            _cache = cache;
            _logger = logger;
        }
        //async Task<IActionResult>


        [HttpPost("dedupjobs/{RuleId}")]
        public async Task<IActionResult> CreateDeduplicationJob(int RuleId, DateRangeDto dto)
        {

            string[] formats = { "yyyy-MM-d" };
            DateTime start, end;
            (bool flowControl, IActionResult value) = IsDateRangeValid(dto, formats, out start, out end);
            if (!flowControl)
            {
                return value;
            }

            try
            {
                int tenantId = User.Tenant();

                var jobId = await _deduplicationJob.CreateJobAsync(tenantId, User.Identifier(), new DeduplicationJobRequest(RuleId, start, end));
                
                _queue.Enqueue(jobId);

                return Ok(new
                {
                    success = true,
                    data = jobId
                });
            }
            catch (SqlException e) when (e.Number >= 60001 && e.Number <= 60002)
            {
                _logger.LogError($"{e.Message}");

                return BadRequest(new
                {
                    success = false,
                    ErrorMessage = e.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while creating deduplication job. {ex.Message}");

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    ErrorMessage = "Error while creating deduplication job"
                });
            }
        }

        [HttpGet("dedupjobs")]
        public async Task<IActionResult> GetDeduplicationJobsPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            if (pageNumber <= 0)
                return BadRequest(new { success = false, ErrorMessage = "pageNumber must be greater than 0." });

            if (pageSize <= 0 || pageSize > 100)
                return BadRequest(new { success = false, ErrorMessage = "pageSize must be between 1 and 100." });


            try
            {
                int tenantId = User.Tenant();

                var result = await _deduplicationJob
                    .GetJobsByTenantPagedAsync(tenantId, pageNumber, pageSize, cancellationToken);

                return Ok(new
                {
                    success = true,
                    totalRecords = result.TotalRecords,
                    totalPages = result.TotalPages,
                    pageNumber,
                    pageSize,
                    data = result.Jobs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching paged targeting jobs. {ex.Message}");

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    ErrorMessage = "Error occurred while fetching targeting jobs."
                });
            }
        }

        [HttpGet("dedupresults/{jobId}")]
        public async Task<IActionResult> GetResults(long jobId,int pageNumber = 1,int pageSize = 10)
        {
            try
            {
                int tenantId = User.Tenant();
                CancellationToken cancellationToken = default;

                var result = await _deduplicationJob.GetSavedResults(
                    tenantId,
                    jobId,
                    pageNumber,
                    pageSize,
                    cancellationToken);

                double minScore = double.MaxValue;
                double maxScore = 0;

                foreach (var m in result.Items)
                {
                    if (m.TotalScore > maxScore)
                        maxScore = m.TotalScore;

                    if (m.TotalScore < minScore)
                        minScore = m.TotalScore;
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        records = result.Items,
                        totalCount = result.TotalCount,
                        minScore,
                        maxScore
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while fetching deduplication results for job {JobId}",
                    jobId);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    errorMessage = $"Error while fetching deduplication results for job {jobId}"
                });
            }
        }

        [HttpPost("disablejob/{jobId}")]
        public async Task<IActionResult> DisableDeduplicationJob(long jobId, CancellationToken cancellationToken = default)
        {
            if (jobId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    ErrorMessage = "Invalid jobId."
                });
            }

            try
            {
                int tenantId = User.Tenant();
                string userId = User.Identifier();

                await _deduplicationJob.ClearDeduplicationResultsAsync(
                    jobId,
                    tenantId,
                    userId,
                    cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Job disabled successfully."
                });
            }
            catch (SqlException ex) when (ex.Number == 70001)
            {
                _logger.LogWarning($"Failed to disable job {jobId}. {ex.Message}");

                return BadRequest(new
                {
                    success = false,
                    ErrorMessage = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error disabling job {jobId}. {ex.Message}");

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    ErrorMessage = "Error occurred while disabling targeting job."
                });
            }
        }




        [HttpPost("findmatches/{RuleId}")]
        public async Task<IActionResult> FindMatches(int RuleId, DateRangeDto dto)
        {
            string[] formats = { "yyyy-MM-d" };
            DateTime start, end;
            (bool flowControl, IActionResult value) = IsDateRangeValid(dto, formats, out start, out end);
            if (!flowControl)
            {
                return value;
            }


            try
            {
                int TenantId = User.Tenant();

                List<Household_Document> household_Documents = new List<Household_Document>();

                string key = $"{TenantId}_{dto.from_date}_{dto.to_date}";

                if (await _cache.ExistsAsync(key))
                {
                    household_Documents = await _cache.GetAsync<List<Household_Document>>(key);
                }
                else
                {
                    household_Documents = await _searchEngine.GetAllHouseholds(TenantId, start, end);
                    TimeSpan ttl = TimeSpan.FromMinutes(DURATION_IN_MIN);
                    await _cache.SetAsync(key, household_Documents, ttl);
                }

                DuplicateCriteriaDefinition rule = await GetDeduplicationRule(TenantId,RuleId);

                DuplicateMatchResultsViewModel matchResults = await
                           _scoringEngine.FindDuplicates(household_Documents, rule);

                return Ok(new
                {
                    success = true,
                    data = matchResults
                });

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while running the scoring process to find duplicates. {ex.Message}");

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    ErrorMessage = "Error while running the scoring process to find duplicates."
                });
            }
        }

        private async Task<DuplicateCriteriaDefinition> GetDeduplicationRule(int TenantId, int ruleId)
        {
            return await _rulesetService.GetCriteriaDefinitionAsync(TenantId, ruleId);
        }

        private (bool flowControl, IActionResult value) IsDateRangeValid(DateRangeDto req, string[] formats, out DateTime start, out DateTime end)
        {
            // Validate start_date format
            if (!DateTime.TryParseExact(req.from_date, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out start))
            {
                end = default;
                return (flowControl: false, value: BadRequest(new
                {
                    success = false,
                    ErrorMessage = "Invalid start_date format. Expected yyyy-MM-d."
                }));
            }

            // Validate end_date format
            if (!DateTime.TryParseExact(req.to_date, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out end))
            {
                return (flowControl: false, value: BadRequest(new
                {
                    success = false,
                    ErrorMessage = "Invalid end_date format. Expected yyyy-MM-d."
                }));
            }

            // Check logical validity
            if (end < start)
            {
                return (flowControl: false, value: BadRequest(
                    new
                    {
                        success = false,
                        ErrorMessage = "end_date cannot be before start_date."
                    }));
            }

            return (flowControl: true, value: null);
        }

    }
}
