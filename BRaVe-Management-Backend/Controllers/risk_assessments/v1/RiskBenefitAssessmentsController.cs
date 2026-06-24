using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class RiskBenefitAssessmentsController : ControllerBase
    {
        private readonly IRiskBenefitAssessment _riskBenefitAssessment;
        public RiskBenefitAssessmentsController(IRiskBenefitAssessment riskBenefitAssessment)
        {
            _riskBenefitAssessment = riskBenefitAssessment;
        }

        [HttpGet("all/{lang}")]
        public async Task<ActionResult<IEnumerable<RiskBenefitAssessment>>> GetAssessments(string lang)
        {
            List<RiskBenefitAssessment> assessments = await _riskBenefitAssessment.GetAllAssessments(User.Tenant(), lang);

            return Ok(assessments);
        }


        [HttpGet("{id}/{lang}")]
        public async Task<ActionResult<RiskBenefitAssessment>> GetAssessment(int id, string lang)
        {
            var tenantId = User.Tenant();

            RiskBenefitAssessmentMetadataDto riskBenefitAssessment = await _riskBenefitAssessment.GetAssessmentById(id, tenantId, lang);

            return Ok(riskBenefitAssessment);
        }


        [HttpPost("create")]
        [Authorize(Roles = "PM")]
        public async Task<ActionResult<RiskBenefitAssessment>> CreateAssessment(DataProcessingDto data)
        {
            var userId = User.Identifier();
            var tenantId = User.Tenant();

            try
            {
                RiskBenefitAssessment riskBenefitAssessment = await _riskBenefitAssessment.CreateRiskBenefitAssessment(new RiskBenefitAssessmentDto { 
                    TenantId = tenantId,
                    ProgramId = data.ProgramId,
                    RequireApprovals = data.RequireApprovals,
                    ProgamManagerContacts = data.ProgamManagerContacts,
                    ProgamManagerName = data.ProgamManagerName,

                            }, userId);
                
                if (riskBenefitAssessment == null)
                    return BadRequest();

                return Ok(riskBenefitAssessment);

            }
            catch (Exception ex) 
            {
                return BadRequest();

            }
            

        }


        [HttpPost("{id}")]
        public async Task<ActionResult<RiskBenefitAssessment>> SubmitAssessment(int id)
        {

            throw new NotImplementedException();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAssessment(int id)
        {

            return NoContent();
        }



    }
}
