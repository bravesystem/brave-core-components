using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PreferencesController : ControllerBase
{
    private readonly ILogger<PreferencesController> _logger;
    private readonly IPreferenceService _preferenceService;

    public PreferencesController(ILogger<PreferencesController> logger, IPreferenceService preferenceService)
    {
        _logger = logger;
        _preferenceService = preferenceService;
    }

    // ───────────────────────────────
    // GET ALL PREFERENCES FOR PROGRAM
    // ───────────────────────────────

    [HttpGet("Tenant/{ProgramId}")]
    public async Task<ActionResult<IEnumerable<Preferences>>> GetAll(int ProgramId)
    {
        _logger.LogInformation("Fetching all consents for ProgramId {ProgramId}", ProgramId);
        var preferences = await _preferenceService.GetAllProgramPreferences(ProgramId);
        _logger.LogInformation("Fetched {Count} consents for ProgramId {ProgramId}", preferences.Count(), ProgramId);
        return Ok(preferences);
    }


    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<Preferences>>> GetAllPreferences()
    {
        _logger.LogInformation("Fetching all preferences");

        try
        {
            var preferences = await _preferenceService.GetDefaultPreferences();
            return Ok(preferences);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all preferences");
            return StatusCode(500, "Error fetching preferences");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MissionPreferences>>> GetPreferencesByTenant()
    {
        int tenantId = User.Tenant();

        _logger.LogInformation("Fetching preferences for TenantId {TenantId}", tenantId);

        var preferences = await _preferenceService.GetAllMissionPreferences(tenantId);

        if (preferences == null)
        {
            _logger.LogWarning("Preferences service returned null for TenantId {TenantId}", tenantId);
            return Ok(new List<MissionPreferences>()); // Return empty list to avoid null reference
        }

        if (!preferences.Any())
        {
            _logger.LogWarning("No preferences found for TenantId {TenantId}", tenantId);
            return Ok(new List<MissionPreferences>()); // Return empty list instead of 404 (better for UI)
        }

        _logger.LogInformation("Fetched {Count} preferences for TenantId {TenantId}", preferences.Count(), tenantId);
        return Ok(preferences);
    }


    [HttpPost("create")]
    public async Task<IActionResult> CreatePreferences()
    {
        try
        {
            int count = await _preferenceService.CreatePreferenceAsync(User.Tenant(), User.Identifier());

            _logger.LogInformation($"Adding default preferences to Mission Id {User.Tenant()}." );

            return Ok(new { message = "Default preferences attached successfully", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create preferences");
            return BadRequest(new { message = "Failed to create preferences", count = 0 });
        }
        
    }


    // change this to use temporary table same as create

    [HttpPost("update")]
    public async Task<IActionResult> UpdatePreferences([FromBody] List<MissionPreferencesDto> dtoList)
    {
        if (dtoList == null || !dtoList.Any())
            return BadRequest("No preferences provided.");

        _logger.LogInformation("Received {Count} mission preferences to update.", dtoList.Count);

        var updateList = dtoList.Select(dto => new MissionPreferences
        {
            PreferenceId = dto.Id ?? 0,                               // <-- PRIMARY KEY (must exist)
            TenantId = dto.TenantId,
            DefaultValue = dto.DefaultValue,
            CreatedByUserId= dto.CreatedByUserId ?? "system",
            CreatedOn = dto.CreatedOn,
            UpdatedByUserId = dto.UpdatedByUserId ?? "system",
            UpdatedOn = DateTime.UtcNow
        }).ToList();

        await _preferenceService.UpdateMissionPreference(updateList);

        return Ok(new { message = "Preferences updated successfully", count = updateList.Count });
    }

    [HttpGet("{tenantId}")]
    public async Task<ActionResult<IEnumerable<MissionPreferences>>> GetPreferencesByTenant(int tenantId)
    {
        _logger.LogInformation("Fetching preferences for TenantId {TenantId}", tenantId);

        var preferences = await _preferenceService.GetAllMissionPreferences(tenantId);

        if (preferences == null)
        {
            _logger.LogWarning("Preferences service returned null for TenantId {TenantId}", tenantId);
            return Ok(new List<MissionPreferences>()); // Return empty list to avoid null reference
        }

        if (!preferences.Any())
        {
            _logger.LogWarning("No preferences found for TenantId {TenantId}", tenantId);
            return Ok(new List<MissionPreferences>()); // Return empty list instead of 404 (better for UI)
        }

        _logger.LogInformation("Fetched {Count} preferences for TenantId {TenantId}", preferences.Count(), tenantId);
        return Ok(preferences);

    }



        //[HttpPost("updateDefault")]
        //public async Task<IActionResult> UpdateDefaultValue([FromBody] UpdateDefaultValueDto dto)
        //{
        //    if (dto.PreferenceId <= 0)
        //        return BadRequest("Invalid PreferenceId");

        //    // Fetch the existing preference
        //    var pref = await _preferenceService.GetPreferenceById(dto.PreferenceId , dto.TenantId);
        //    if (pref == null) return NotFound();

        //    //pref.DefaultValue = dto.DefaultValue;
        //    //await _preferenceService.UpdatePreference(pref);

        //    return Ok(new { message = "Default Value updated" });
        //}






    }





