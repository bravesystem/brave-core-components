using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Helpers.Mock;
using BRaVe_Management_Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class TargetingController : ControllerBase
    {
        private readonly IndicatorComputationService _computationService;

        //public TargetingController()
        //{
        //   // var repo = new MockIndicatorRepository();
        //    //var resolver = new MockRawIndicatorResolver();
        //    var evaluator = new CompositeEvaluator();

        //    _computationService = new IndicatorComputationService(
        //        //repo,
        //        //resolver,
        //        evaluator
        //    );
        //}

        // =========================
        // PREVIEW TARGETING RUN
        // =========================
        [HttpPost("preview")]
        public async Task<IActionResult> Preview([FromBody] TargetingRunDto run)
        {
            var entityIds = new List<Guid>
            {
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid()
            };

            int TenantId = User.Tenant();

            var computed = await _computationService.ComputeAsync(TenantId,run, entityIds);

            var results = computed
                .GroupBy(c => c.EntityId)
                .Select(g => new
                {
                    EntityId = g.Key,
                    Indicators = g.Select(i => new
                    {
                        i.IndicatorId,
                        Value = i.Value
                    }),
                    TotalScore = g.Sum(i => i.Value ?? 0),
                    Eligible = g.Sum(i => i.Value ?? 0) > 50,
                    Rank = 1,
                    Badges = g.Select(i => new
                    {
                        Indicator = i.IndicatorId,
                        Value = i.Value
                    })
                });

            return Ok(new
            {
                runId = run.RunId,
                caseload = results.Count(),
                results
            });
        }
    }
}
