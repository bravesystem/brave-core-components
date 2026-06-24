using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class DuplicateRulesetsController : ControllerBase
    {
        private readonly IDuplicateRulesetService _duplicateRulesetService;
        private readonly ILogger<DuplicateRulesetsController> _logger;

        public DuplicateRulesetsController(IDuplicateRulesetService duplicateRulesetService, ILogger<DuplicateRulesetsController> logger)
        {
            _duplicateRulesetService = duplicateRulesetService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DuplicateRulesetViewModel>>> GetAll()
        {
            int TenantId = User.Tenant();
            var rules = await _duplicateRulesetService.GetAll(TenantId);
            return Ok(rules);
        }


        [HttpGet("predicates/{RulesetId}")]
        public async Task<ActionResult<DuplicatePredicateViewModel>> GetPredicates(int RulesetId)
        {
            int TenantId = User.Tenant();
            var predicates = await _duplicateRulesetService.GetPredicates(TenantId, RulesetId);
            return Ok(predicates);
        }

        [HttpPost("identity/compare")]
        public async Task<ActionResult<PredicateEvaluationViewModel>> Compare(IndividualPairDto dto)
        {
            int TenantId = User.Tenant();
            var evaluations = await _duplicateRulesetService.CompareIndividuals(TenantId, dto);
            return Ok(evaluations);
        }
    }
}
