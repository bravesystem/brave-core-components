using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.ComponentModel.Design;
using System.Data.Common;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class ActivitiesController : ControllerBase
    {
        private readonly IActivityService _activityService;
        private readonly ILogger<ActivitiesController> _logger;

        public ActivitiesController(IActivityService activityService, ILogger<ActivitiesController> logger)
        {
            _activityService = activityService;
            _logger = logger;
        }

        [HttpGet("Program/{programId}")]
        public async Task<ActionResult<IEnumerable<RegistrationActivity>>> GetAll(int programId)
        {
            _logger.LogInformation("Fetching all activities for ProgramId {ProgramId}", programId);
            var activities = await _activityService.GetAllActivities(programId);
            return Ok(activities);
        }

        [HttpGet("{activityId}")]
        public async Task<ActionResult<RegistrationActivity>> GetById(int activityId)
        {
            _logger.LogInformation("Fetching activity with Id {ActivityId}", activityId);
            var activity = await _activityService.GetActivityById(activityId);
            if (activity == null)
            {
                _logger.LogWarning("Activity with Id {ActivityId} not found", activityId);
                return NotFound();
            }
            return Ok(activity);
        }

        [HttpPost("validate/{activityId}")]
        public async Task<IActionResult> Validate(int activityId)
        {

            _logger.LogInformation("Activating activity with Id {ActivityId}", activityId);

            try
            {
                await _activityService.Validate(activityId, User.Tenant(), User.Identifier());
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validat activity {ActivityId}", activityId);
                return StatusCode(500, "Failed to activate activity");
            }
        }

        [HttpPost("invalidate/{activityId}")]
        public async Task<IActionResult> Invalidate(int activityId)
        {

            _logger.LogInformation("De-activating activity with Id {ActivityId}", activityId);

            try
            {
                await _activityService.Invalidate(activityId, User.Tenant(), User.Identifier());
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to de-activate activity {ActivityId}", activityId);
                return StatusCode(500, "Failed to de-activate activity");
            }
        }

        [HttpGet("{activityId}/DataPoints")]
        public async Task<ActionResult<IEnumerable<ActivityDataPoint>>> GetDataPointsForActivity(int activityId)
        {
          
            var dataPoints = await _activityService.GetDataPointsForActivity(activityId);
            return Ok(dataPoints);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RegistrationActivity newActivity)
        {
            if (newActivity == null)
                return BadRequest();

            // Fetch all activities for the program to calculate next ActivityCode
            //var activities = await _activityService.GetAllActivities(newActivity.ProgramId);
            //int nextNumber = 1001;

            /*if (activities.Any())
            {
                var maxCodeNumber = activities
                    .Select(a =>
                    {
                        if (!string.IsNullOrEmpty(a.ActivityCode) && a.ActivityCode.StartsWith("ACT") &&
                            int.TryParse(a.ActivityCode.Substring(3), out int num))
                        {
                            return num;
                        }
                        return 1000;
                    })
                    .Max();

                nextNumber = maxCodeNumber + 1;
            }*/

            // Assign the new ActivityCode
            //newActivity.ActivityCode = $"ACT{nextNumber}";

            // Save activity
            await _activityService.CreateActivity(newActivity);

            return CreatedAtAction(nameof(GetById), new { activityId = newActivity.ActivityId }, newActivity);
        }


        [HttpPut] 
        public async Task<IActionResult> Update([FromBody] RegistrationActivity updatedActivity)
        {
            if (updatedActivity == null || updatedActivity.ActivityId <= 0)
                return BadRequest("Invalid activity data");

            _logger.LogInformation("Updating activity with Id {ActivityId}", updatedActivity.ActivityId);

            try
            {
                await _activityService.UpdateActivity(updatedActivity);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update activity {ActivityId}", updatedActivity.ActivityId);
                return StatusCode(500, "Failed to update activity");
            }
        }

        [HttpPost("{activityId}/DataPoints/{dataPointId}/DatapointType/{dataPointType}")]
        public async Task<IActionResult> AttachDataPointToActivity(int activityId, int dataPointId,int dataPointType, [FromBody] RequiredDto data)
        {
            try
            {
                data.TenantId = User.Tenant();
                await _activityService.AttachDataPointAsync(activityId, dataPointId, dataPointType, data);

                _logger.LogInformation(
                    "Attached DataPoint {DataPointId} to Activity {ActivityId}",
                    dataPointId, activityId);

                return Ok(new { Message = "DataPoint successfully attached to Activity" });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error attaching DataPoint {DataPointId} to Activity {ActivityId}",
                    dataPointId, activityId);

                return StatusCode(500, "Error attaching DataPoint to Activity");
            }
        }

        [HttpDelete("{activityId}/DataPoints/{dataPointId}")]
        public async Task<IActionResult> DetachDataPointFromActivity(int activityId, int dataPointId)
        {
            try
            {
                await _activityService.DetachDataPointAsync(activityId, dataPointId);
                _logger.LogInformation("Detached DataPoint {DataPointId} from Activity {ActivityId}", dataPointId, activityId);
                return Ok(new { Message = "DataPoint successfully detached from Activity" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detaching DataPoint {DataPointId} from Activity {ActivityId}", dataPointId, activityId);
                return StatusCode(500, "Error detaching DataPoint from Activity");
            }
        }

        [HttpGet("{activityId}/Surveys")]
        public async Task<ActionResult<IEnumerable<ActivitySurveys>>> GetActivitySurveys(int activityId)
        {
            var surveys = await _activityService.GetSurveysForActivity(activityId);
            return Ok(surveys);
        }


        // ───────────────────────────────
        // CONSENTS
        // ───────────────────────────────

        [HttpPost("{activityId}/Consents/{consentId}/ConsentType/{consentType}")]
        public async Task<IActionResult> AddConsentToActivity(int activityId, int consentId, int consentType, [FromBody] RequiredDto data)
        {
            _logger.LogInformation("Attaching consent {ConsentId} to activity {ActivityId}", consentId, activityId);
         
            try
            {
                data.TenantId = User.Tenant();
                await _activityService.AttachConsentAsync(activityId, consentId, consentType, data);
                _logger.LogInformation("Consent {ConsentId} attached to activity {ActivityId}", consentId, activityId);
                return Ok(new { Message = "Consent successfully attached to Activity" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attaching Consent {ConsentId} to Activity {ActivityId}", consentId, activityId);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{activityId}/Consents")]
        public async Task<IActionResult> GetConsentsForActivity(int activityId,string languageCode)
        {
            _logger.LogInformation("Fetching consents for activity {ActivityId}", activityId);

            int TenantId = User.Tenant();

            var consents = await _activityService.GetConsentForActivity(TenantId,activityId,  languageCode);
            return Ok(consents);
        }


        [HttpDelete("{activityId}/Consents/{consentId}")]
        public async Task<IActionResult> RemoveConsentFromActivity(int activityId, int consentId)
        {
            _logger.LogInformation("Removing consent {ConsentId} from activity {ActivityId}", consentId, activityId);

            try
            {
                await _activityService.RemoveAttachedConsentAsync(activityId, consentId);

                _logger.LogInformation("Consent {ConsentId} removed from activity {ActivityId}", consentId, activityId);
                return Ok(new { Message = "Consent successfully removed from activity" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Activity or Consent not found");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing consent {ConsentId} from activity {ActivityId}", consentId, activityId);
                return StatusCode(500, "Error removing consent from activity");
            }
        }

        [HttpPost("{activityId}/Surveys/{surveyId}")]
        public async Task<IActionResult> AddSurveyToActivity(int activityId, int surveyId, [FromBody] RequiredDto data)
        {
            data.TenantId = User.Tenant();
            await _activityService.AttachSurveyAsync(activityId, surveyId,data);
            return Ok(new { Message = "Survey successfully attached to activity" });
        }

        [HttpDelete("{activityId}/Surveys/{surveyId}")]
        public async Task<IActionResult> RemoveSurveyFromActivity(int activityId, int surveyId)
        {
            _logger.LogInformation("Removing survey {SurveyId} from activity {ActivityId}", surveyId, activityId);

            try
            {
                await _activityService.RemoveAttachedSurveyAsync(activityId, surveyId);

                _logger.LogInformation("Survey {SurveyId} removed from activity {ActivityId}", surveyId, activityId);
                return Ok(new { Message = "Survey successfully removed from activity" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Activity or Survey not found");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing survey {SurveyId} from activity {ActivityId}", surveyId, activityId);
                return StatusCode(500, "Error removing survey from activity");
            }
        }

       
        [HttpPost("{activityId}/preferences")]
        public async Task<IActionResult> AttachPreferenceToActivity(
           int activityId,
           [FromBody] List<MissionPreferencesDto> dtoList)
        {
            if (dtoList == null || !dtoList.Any())
                return BadRequest("No preferences provided.");

            _logger.LogInformation(
                "Received {Count} preferences for ActivityId {ActivityId}.",
                dtoList.Count, activityId);

            var createdList = new List<MissionPreferences>();

            foreach (var dto in dtoList)
            {
                var newPref = new MissionPreferences
                {
                    //ActivityId = activityId, // 🔑 IMPORTANT
                    PreferenceId = dto.Id ?? 0,
                    PreferenceType= dto.PreferenceType,
                    TenantId = dto.TenantId,
                    DefaultValue = dto.DefaultValue,
                    CreatedByUserId = dto.CreatedByUserId ?? "system",
                    CreatedOn = DateTime.UtcNow,
                    UpdatedByUserId = dto.UpdatedByUserId ?? "system",
                    UpdatedOn = DateTime.UtcNow
                };

                createdList.Add(newPref);
            }

            if (!createdList.Any())
                return BadRequest("No valid preferences to insert.");

            try
            {
                await _activityService.AttachPreferenceAsync(activityId, createdList);

                _logger.LogInformation(
                    "Attached {Count} preferences to ActivityId {ActivityId}.",
                    createdList.Count, activityId);

                return Ok(new
                {
                    message = "Preferences attached successfully",
                    activityId,
                    count = createdList.Count
                });
            }
            catch (DbException ex)// when (ex.Number==60010)
            {
                if (ex is SqlException sqlEx && ((SqlException)ex).Number == 60010)
                {
                    return BadRequest(ex.Message);
                }

                return BadRequest("The record could not be updated due to a database error.");


            }
            catch (Exception ex) 
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

            
        }




        [HttpGet("{activityId}/preference")]
        public async Task<ActionResult<IEnumerable<ActivityPreference>>> GetAttachedPreferences(int activityId, int TenantId)
        {
            var preference = await _activityService.GetAttachedPreferences(activityId, TenantId);
            return Ok(preference);
        }


        [HttpPost("{activityId}/Enumerators/bulk")]
        public async Task<IActionResult> AttachEnumeratorsBulk(
            int activityId,
            [FromBody] List<string> enumeratorCodes)
        {
            if (enumeratorCodes == null || !enumeratorCodes.Any())
                return BadRequest("No enumerators provided.");

            try
            {
                var tenantId = User.Tenant();

                await _activityService.AttachEnumeratorsBulkAsync(
                    activityId,
                    enumeratorCodes,
                    tenantId
                );

                _logger.LogInformation(
                    "Attached {Count} enumerators to Activity {ActivityId}",
                    enumeratorCodes.Count,
                    activityId);

                return Ok(new
                {
                    message = "Enumerators attached successfully",
                    count = enumeratorCodes.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error attaching enumerators to Activity {ActivityId}",
                    activityId);

                return StatusCode(500, "Error attaching enumerators to Activity");
            }
        }

        [HttpGet("{activityId}/Enumerator")]
        public async Task<ActionResult<IEnumerable<ActivityEnumerator>>> GetEnumeratorsForActivity(int activityId)
        {
            try
            {
                // Fetch enumerators from the activity service
                var enumerators = await _activityService.GetEnumeratorsForActivity(activityId)
                                    ?? new List<ActivityEnumerator>();

                return Ok(enumerators);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching enumerators for Activity {ActivityId}", activityId);
                return StatusCode(500, "Error fetching enumerators for activity");
            }
        }

        // DELETE: v1/Activities/{activityId}/DefaultPreferences/{id}
        [HttpDelete("{activityId}/DefaultPreferences/{id}")]
        public async Task<IActionResult> RemoveDefaultPreference(int activityId, int id)
        {
            try
            {
                await _activityService.RemovePreferenceAsync(activityId, id);
                return Ok(new { message = "Default preference removed successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Preference not found for Activity {ActivityId}, Preference {PreferenceId}", activityId, id);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing preference {PreferenceId} from Activity {ActivityId}", id, activityId);
                return StatusCode(500, new { error = "Unable to remove default preference—please try again later." });
            }
        }
        // DELETE: v1/Activities/{activityId}/Enumerators/{id}
        [HttpDelete("{activityId}/Enumerators/{enumeratorCode}")]
        public async Task<IActionResult> RemoveEnumeratorFromActivity(int activityId, string enumeratorCode)
        {
            try
            {
                await _activityService.RemoveEnumeratorAsync(activityId, enumeratorCode);
                _logger.LogInformation("Enumerator {EnumeratorId} removed from Activity {ActivityId}", enumeratorCode, activityId);
                return Ok(new { message = "Enumerator removed successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Enumerator not found for Activity {ActivityId}, Enumerator {EnumeratorId}", activityId, enumeratorCode);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing enumerator {EnumeratorId} from Activity {ActivityId}", enumeratorCode, activityId);
                return StatusCode(500, new { error = "Unable to remove enumerator—please try again later." });
            }
        }


        [HttpPost("{activityId}/Distributions/{distributionId}/DistributionType/{distributionType}")]
        public async Task<IActionResult> AttachDistributionsToActivity(int activityId, int distributionId, int distributionType, int EnrollmentMode,bool PhotoConfirmation,
    bool BiometricVerification, [FromBody] RequiredDto data)
        {
            try
            {
                data.TenantId = User.Tenant();
                await _activityService.AttachDistributionsAsync(activityId, distributionId, distributionType, EnrollmentMode, PhotoConfirmation, BiometricVerification, data);
                _logger.LogInformation("Attached Distributions {DistributionId} to Activity {ActivityId}", distributionId, activityId);
                return Ok(new { Message = "Distributions successfully attached to Activity" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attaching Distributions {Distributions} to Activity {ActivityId}", distributionId, activityId);
                return StatusCode(500, "Error attaching Distributions to Activity");
            }
        }


        [HttpGet("{activityId}/Distributions")]
        public async Task<ActionResult<IEnumerable<ActivityDistributions>>> GetDistributionsForActivity(int activityId)
        {
           
            var Distributions = await _activityService.GetDistributionsForActivity(activityId);
            return Ok(Distributions);
        }


        [HttpDelete("{activityId}/Distributions/{distributorId}")]
        public async Task<IActionResult> DetachDistributionsFromActivity(int activityId, int distributorId)
        {
            try
            {
                await _activityService.DetachDistributionsAsync(activityId, distributorId);
                _logger.LogInformation("Detached Distributions {DistributionsId} from Activity {ActivityId}", distributorId, activityId);
                return Ok(new { Message = "Distributions successfully detached from Activity" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detaching Distribute {DistributerId} from Activity {ActivityId}", distributorId, activityId);
                return StatusCode(500, "Error detaching Distributions from Activity");
            }
        }


    }

}

