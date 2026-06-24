using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        // GET: api/v1/dashboard
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DashboardRowDto>>> Get(int? programId,int? activityId,DateTime? startDate,DateTime? endDate)
        {
            int tenantId = User.Tenant();

            try
            {
                var results = await _dashboardService.GetDashboardDataAsync(
                    tenantId,
                    programId,
                    activityId,
                    startDate,
                    endDate);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                return StatusCode(500, "Failed to load dashboard data");
            }
        }

        // GET: api/v1/dashboard/survey-details
        [HttpGet("survey-details")]
        public async Task<ActionResult<IEnumerable<DashboardSurveyDetailDto>>> GetSurveyDetails(string? householdId,int? individualId)
        {
            try
            {
                var results = await _dashboardService
                    .GetDashboardSurveyDetailsAsync(householdId, individualId);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard survey details");
                return StatusCode(500, "Failed to load dashboard survey details");
            }
        }


        // GET: api/v1/dashboard/households
        [HttpGet("households/{householdId}")]
        public async Task<IActionResult> GetHousehold(string householdId)

        {

            int tenantId = User.Tenant();
            var household = await _dashboardService.GetHouseholdByIdAsync(householdId, tenantId);

            if (household == null)
                return NotFound();

            return Ok(household);
        }


        // GET: api/v1/dashboard/households/{id}/members
        [HttpGet("households/{householdId}/members")]
        public async Task<ActionResult<IEnumerable<HouseholdMemberDto>>> GetHouseholdMembers(
    string householdId)
        {

            try
            {
                var members = await _dashboardService
                    .GetHouseholdMembersAsync(householdId);

                return Ok(members);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to load members for HouseholdId {HouseholdId}",
                    householdId);

                return StatusCode(500, "Failed to load household members.");
            }
        }

        [HttpGet("graphdata")]
        public async Task<IActionResult> GetGraphData(int? tenantId,
            int? programId,
            DateTime? startDate,
            DateTime? endDate)
        {
            //int tenantId = User.Tenant();
            try
            {
                var data = await _dashboardService.GetDashboardGraphDataAsync(
                    tenantId,
                    programId,
                    startDate,
                    endDate);

                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard graph data");
                return StatusCode(500, "Failed to load dashboard graph data");
            }
        }




    }
}
