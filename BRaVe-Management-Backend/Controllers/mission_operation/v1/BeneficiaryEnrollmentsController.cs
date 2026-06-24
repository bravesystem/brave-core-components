using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

namespace BRaVe_Management_Backend.Controller
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class BeneficiaryEnrollmentsController : ControllerBase
    {
        private readonly IBeneficiaryEnrollmentsService _service;
        private readonly ILogger<BeneficiaryEnrollmentsController> _logger;


        public BeneficiaryEnrollmentsController(
            IBeneficiaryEnrollmentsService service,
            ILogger<BeneficiaryEnrollmentsController> logger)
        {
            _service = service;
            _logger = logger;
        }




        // GET: api/v1/BeneficiaryEnrollments
        [HttpGet()]
        public async Task<IActionResult> GetAllDistributions()
        {
            try
            {
                // Assuming you store TenantId in claims
                var tenantId = User.Tenant();

                var data =
                    await _service.GetBeneficiaryAllEnrollmentsAsync(
                        tenantId);

                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching enrollments");

                return StatusCode(500,
                    "Failed to load enrollments");
            }
        }

        // GET: api/v1/BeneficiaryEnrollments/{distributionId}
        [HttpGet("{distributionId}")]
        public async Task<IActionResult> GetByDistribution(int distributionId)
        {
            try
            {
                // Assuming you store TenantId in claims
                var tenantId = User.Tenant();

                var data =
                    await _service.GetBeneficiaryEnrollmentsAsync(
                        tenantId,
                        distributionId);

                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching enrollments");

                return StatusCode(500,
                    "Failed to load enrollments");
            }
        }

        [HttpGet("{distributionId}/{individualId}/{householdId}")]
        public async Task<IActionResult> GetByIds(int distributionId, int individualId, string householdId)
        {
            var tenantId = User.Tenant();

            var result = await _service.GetEnrollmentByIdsAsync(
                tenantId,
                distributionId,
                individualId,
                householdId);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

    }
}
