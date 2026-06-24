using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Helpers.Mock;
using BRaVe_Management_Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class TargetingCriteriasController : ControllerBase
    {
        private readonly ITargetingCriteriaService _repo;

        public TargetingCriteriasController(ITargetingCriteriaService repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var all = await _repo.GetAllAsync(); // implement in your repo
            return Ok(all);
        }
        // GET BY ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _repo.GetByIdAsync(id);

            if (item == null)
                return NotFound();

            return Ok(item);
        }


        // GET BY RULE
        [HttpGet("by-rule/{ruleId}")]
        public async Task<IActionResult> GetByRule(int ruleId)
        {
            return Ok(await _repo.GetByTargetingIdAsync(ruleId));
        }

        // CREATE
        [HttpPost]
        public async Task<IActionResult> Create(TargetingCriteriaDto dto)
        {
            var saved = await _repo.CreateAsync(dto);
            return Ok(saved);
        }

        // UPDATE
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, TargetingCriteriaDto dto)
        {
            if (id != dto.Id)
                return BadRequest();

            var saved = await _repo.UpdateAsync(id,dto);
            return Ok(saved);
        }

        // DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }
        [HttpGet("lookup/{lookupId:int}/tenant/{tenantId:int}")]
        public async Task<ActionResult<List<TargetingFieldValueDto>>> GetLookupValues(int lookupId, int tenantId)

        {

            var values = await _repo.GetLookupValuesAsync(lookupId, tenantId);

            if (values == null || !values.Any())
                return Ok(new List<TargetingFieldValueDto>());

            return Ok(values);
        }
    }
}

