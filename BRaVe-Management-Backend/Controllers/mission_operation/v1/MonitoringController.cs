using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Exceptions;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Win32;
using StackExchange.Redis;
using System;
using static BRaVe_Management_Backend.Helpers.KeyVaultSecretNames;


namespace BRaVe_Management_Backend.Controller
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class MonitoringController : ControllerBase
    {
        private readonly IMonitoringService _service;
        private readonly ILogger<MonitoringController> _logger;

        public MonitoringController(
            IMonitoringService service,
            ILogger<MonitoringController> logger)
        {
            _service = service;
            _logger = logger;
        }
        [HttpGet]
        public async Task<ActionResult<List<FailedJobGridRowDto>>> GetFailedJobs([FromQuery] int? tenantId)
        {
            try
            {
                var result = await _service.GetFailedBatchesAsync(tenantId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching failed jobs");
                return StatusCode(500, "Error fetching failed jobs");
            }
        }
        [HttpPost("{jobId:guid}/retry")]
        public async Task<IActionResult> RetryJob(Guid jobId)
        {
            await _service.HandleAsync(jobId);
            return Ok();
        }
    }
}