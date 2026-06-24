using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using BRaVe_Management_Backend.Models.es;
using BRaVe_Portal.Models;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Globalization;

namespace BRaVe_Management_Backend.Controllers.search_analytics
{
    [Route("api/v1/[controller]")]
    [ApiController]
    //[Authorize]
    public class SearchEngineController : ControllerBase
    {
        //private readonly ISearchEngine _searchEngine;
        private readonly ISearchService _searchService;
        private readonly IScoringEngine _scoringEngine;
        private readonly IAppCache _cache;
        private readonly ILogger<SearchEngineController> _logger;

        private readonly ITargetingJob _targetingJob;
        private readonly ITargetingJobQueue _queue;

        public static int DURATION_IN_MIN = 10;

        public SearchEngineController(
            //ISearchEngine searchEngine,
            ISearchService searchService,
            IScoringEngine scoringEngine,
            ITargetingJob targetingJob,
            ITargetingJobQueue queue,
            IAppCache cache,
            ILogger<SearchEngineController> logger)
        {
            //_searchEngine = searchEngine;
            _searchService = searchService;
            _scoringEngine = scoringEngine;

            _targetingJob = targetingJob;
            _queue = queue;

            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Creates a targeting job and returns JobId. Processing runs asynchronously in the background.
        /// </summary>
        [HttpPost("targetingjob")]
        public async Task<IActionResult> CreateJob([FromBody] TargetingRequest request, CancellationToken cancellationToken)
        {
            string[] formats = { "yyyy-MM-d" };
            DateTime start, end;
            (bool flowControl, IActionResult value) = IsDateRangeValid(new DateRangeDto { 
                from_date = request.range.from_date,
                to_date = request.range.to_date,
            }, formats, out start, out end);

            if (!flowControl)
            {
                return value;
            }

            try
            {
                var jobId = await _targetingJob.CreateJobAsync(User.Tenant(), User.GetUserEmailLike(), new TargetingJobRequest(
                    request.targetingId, start, end
                ), cancellationToken);
                _queue.Enqueue(jobId);
                return Ok(new
                {
                    success = true,
                    JobId = jobId
                });
            }
            catch (SqlException e)when (e.Number >= 60001 && e.Number <= 60002)
            {
                _logger.LogError($"{e.Message}");

                return BadRequest(new
                {
                    success = false,
                    ErrorMessage = e.Message
                });
            }

            
        }

        /// <summary>
        /// Returns paginated targeting jobs for the current tenant.
        /// </summary>
        [HttpGet("targetingjobs")]
        public async Task<IActionResult> GetTargetingJobsPaged(
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

                var result = await _targetingJob
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

        /// <summary>
        /// Returns targeting results based on the passed job id
        /// </summary>

        [HttpGet("targetingresults/{jobId}")]
        public async Task<IActionResult> GetTargetingResults(long jobId)
        {
            try
            {
                int tenantId = User.Tenant();

                var results = await _searchService
                    .GetSavedScorings(tenantId, jobId);

                return Ok(new
                {
                    success = true,
                    data = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching saved targeting results. {ex.Message}");

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    ErrorMessage = "Error fetching targeting results."
                });
            }
        }


        [HttpPost("scoring")]
        public async Task<IActionResult> GetHouseholdScoresForRange(TargetingRequest req) {

            string[] formats = { "yyyy-MM-d" };
            DateTime start, end;
            (bool flowControl, IActionResult value) = IsDateRangeValid(req.range, formats, out start, out end);
            if (!flowControl)
            {
                return value;
            }


            try
            {
                int TenantId = User.Tenant();

                List<Household_Document> household_Documents = new List<Household_Document>();

                string key = $"{TenantId}_{req.range.from_date}_{req.range.to_date}";

                if (await _cache.ExistsAsync(key))
                {

                    household_Documents = await _cache.GetAsync<List<Household_Document>>(key);

                }
                else 
                {
                    household_Documents = await _searchService.GetAllHouseholds(TenantId, start, end);
                    TimeSpan ttl = TimeSpan.FromMinutes(DURATION_IN_MIN);
                    await _cache.SetAsync(key, household_Documents, ttl);

                }

                RuleDefinition ruleDefinition = await GetRuleDefinition(req.targetingId);

                List<ScoredResult> scoredResults = await
                           _scoringEngine.SearchAsync(household_Documents, ruleDefinition);

                return Ok(scoredResults);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error while running the scoring engine. {e.Message}");
            }

            return BadRequest(new
            {
                success = false,
                ErrorMessage = "Error while running the scoring engine."
            });

        }

        private async Task<RuleDefinition> GetRuleDefinition(int targetingId)
        {
            return await _searchService.GetRuleDefinition(targetingId);
        }

        [HttpPost("bydaterange")]
        public async Task<IActionResult> GetHouseholdsByDateRange(DateRangeDto rng)
        {
            string[] formats = { "yyyy-MM-d" };
            DateTime start, end;
            (bool flowControl, IActionResult value) = IsDateRangeValid(rng, formats, out start, out end);
            if (!flowControl)
            {
                return value;
            }

            try
            {
                int TenantId = User.Tenant();

                List<Household_Document> household_Documents = new List<Household_Document>();

                string key = $"{TenantId}_{rng.from_date}_{rng.to_date}";

                if (await _cache.ExistsAsync(key))
                {
                    household_Documents = await _cache.GetAsync<List<Household_Document>>(key);
                }
                else
                {
                    household_Documents = await _searchService.GetAllHouseholds(TenantId, start, end);
                    TimeSpan ttl = TimeSpan.FromMinutes(DURATION_IN_MIN);
                    await _cache.SetAsync(key, household_Documents, ttl);
                }

                return Ok(household_Documents);
            }
            catch (SqlException e) when (e.Number >= 60001 && e.Number <= 60009)
            {
                 _logger.LogError($"{e.Message}");

                return BadRequest(new
                {
                    success = false,
                    ErrorMessage = e.Message
                });
            }
            catch (Exception e)
            {
                _logger.LogError($"Error occurred while fetching data. {e.Message}");
            }

            return BadRequest(new
            {
                success = false,
                ErrorMessage = "Error occurred while fetching data."
            });

        }


        /// <summary>
        /// Disables a completed targeting job and deletes its results.
        /// </summary>
        [HttpPost("disablejob")]
        public async Task<IActionResult> DisableTargetingJob( [FromQuery] long jobId, CancellationToken cancellationToken = default)
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
                string userId = User.GetUserEmailLike();

                await _targetingJob.ClearTargetingResultsAsync(
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
            catch (SqlException ex) when(ex.Number==70001)
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
