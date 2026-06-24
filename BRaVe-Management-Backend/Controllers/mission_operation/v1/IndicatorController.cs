using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Helpers.Mock;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BRaVe_Management_Backend.Extensions;

namespace BRaVe_Management_Backend.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class IndicatorController : ControllerBase
    {
        private readonly IIndicatorRepositoryService _repository;

        // MOCK wiring (later replaced by DI + DB)
        public IndicatorController(IIndicatorRepositoryService repository)
        {
            _repository = repository;
        }

        // =========================
        // GET ALL INDICATORS
        // =========================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            int TenantId = User.Tenant();
            var indicators = await _repository.GetActiveIndicatorsAsync(TenantId);
            return Ok(indicators);
        }

        // =========================
        // GET BY CODE
        // =========================
        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var indicator = await _repository.GetByCodeAsync(code);
            if (indicator == null)
                return NotFound();

            return Ok(indicator);
        }

        // =========================
        // CREATE INDICATOR
        // =========================
        [HttpPost]
        public IActionResult Create([FromBody] IndicatorDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // MOCK: just echo back
            dto.IndicatorId = new Random().Next(100, 999);
            dto.IsActive = true;

            return Ok(dto);
        }

        // =========================
        // DEACTIVATE INDICATOR
        // =========================
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            // MOCK: no persistence yet
            return NoContent();
        }

        [HttpPost("composite")]
        public async Task<IActionResult> SaveComposite([FromBody] IndicatorDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var saved = await _repository.SaveCompositeAsync(User.Tenant(), User.Identifier(), dto);

            return Ok(saved);
        }

        // =========================
        // UPDATE INDICATOR
        // =========================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] IndicatorDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Ensure the ID matches
            if (dto.IndicatorId != id)
                return BadRequest("Indicator ID mismatch.");

            // MOCK: retrieve existing indicator (replace with DB in real implementation)
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound();

            // Update fields
            //existing.Code = dto.Code;
            existing.Name = dto.Name;
            existing.Level = dto.Level;
            existing.DataType = dto.DataType;
            existing.Description = dto.Description;
            existing.Expression = dto.Expression;
            existing.JsonRule = dto.JsonRule;
            existing.IndicatorType = dto.IndicatorType;
            existing.IsActive = dto.IsActive;

            // Save changes (mock)
       
            await _repository.UpdateAsync(existing.IndicatorId, User.Tenant(), User.Identifier(), existing);


            return Ok(existing);
        }
   

    }
}
