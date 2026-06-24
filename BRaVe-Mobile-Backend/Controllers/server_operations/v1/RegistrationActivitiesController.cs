using BRaVe_Mobile_Backend.Controllers.devices_onboarding;
using BRaVe_Mobile_Backend.DTOs;
using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;
using BRaVe_Mobile_Backend.Models;
using BRaVe_Mobile_Backend.Models.data_payload;
using BRaVe_Mobile_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Serilog;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BRaVe_Mobile_Backend.Controllers.server_operations
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class RegistrationActivitiesController : ControllerBase
    {
        private readonly IPubService _pubService;
        private readonly IRequestContextValidator _contextValidator;
        private readonly IRegistrationActivityService _registrationActivityService;
        private readonly ILogger<RegistrationActivitiesController> _logger;

        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceBusSender _bus;

        public RegistrationActivitiesController(
                                IBackgroundTaskQueue taskQueue,
                                IServiceScopeFactory scopeFactory,
                                IPubService pubService,
                                IServiceBusSender bus,
                                IRequestContextValidator contextValidator, 
                                IRegistrationActivityService registrationActivityService, 
                                ILogger<RegistrationActivitiesController> logger) 
        {

            _taskQueue = taskQueue;
            _scopeFactory = scopeFactory;

            _pubService = pubService;
            _contextValidator = contextValidator;
            _registrationActivityService = registrationActivityService;
            _bus = bus;
            _logger = logger;
           
        }

        [HttpPost("download/flaggeddata/{tenantId}/{partitionCode}")]
        public async Task<ActionResult<FlaggedData>> DownloadFlaggedData(
            int tenantId,
            string partitionCode)
        {
            try
            {
                _logger.LogInformation(
                    "Downloading flagged data. TenantId: {TenantId}, PartitionCode: {PartitionCode}",
                    tenantId,
                    partitionCode);


                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

                //return Ok(new List<BiometricVerificationResponse>());
                var context = await _contextValidator.ValidateAsync(User, ip);
                if (!context.Ok)
                    return StatusCode(context.StatusCode, new { error = context.Error });

                DeviceProfile deviceProfile = _contextValidator.DeviceProfile();

                var result =
                    await _registrationActivityService
                        .DownloadFlaggedData(
                            tenantId,
                            partitionCode,
                            deviceProfile.DeviceId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error downloading flagged data for partition {PartitionCode}",
                    partitionCode);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Failed to download flagged data.");
            }
        }


        [HttpGet("download/{tenantId}/{activityId}")]
        public async Task<ActionResult<RegistrationActivity>> Download(int tenantId, string activityId, [FromQuery] string lang = "en")
        {
            try
            {

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

                //return Ok(new List<BiometricVerificationResponse>());
                var result = await _contextValidator.ValidateAsync(User, ip);
                if (!result.Ok)
                    return StatusCode(result.StatusCode, new { error = result.Error });

                DeviceProfile deviceProfile = _contextValidator.DeviceProfile();


                return await _registrationActivityService.GetActivity(deviceProfile.DeviceId,activityId, tenantId, lang);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, ex.Message);
            }

            return NotFound(new
            {
                error = $"Activity ID {activityId} not found"
            });
        }

        [HttpPost("enrollments/fetch")]
        public async Task<IActionResult> fetchEnrollmentData([FromBody] DataPacket data)
        {
            try
            {
                _logger.LogInformation("Receiving Enrollment request..");


                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

                //return Ok(new List<BiometricVerificationResponse>());
                var result = await _contextValidator.ValidateAsync(User, ip);
                if (!result.Ok)
                    return StatusCode(result.StatusCode, new { error = result.Error });

                DeviceProfile deviceProfile = _contextValidator.DeviceProfile();

                // Load the public key from database
                byte[] derBytes = await _pubService.getClientPublickey(deviceProfile.DeviceId); // PEM format

                string publicKeyPem = PemHelper.DerBytesB64(derBytes);

                using RSA rsa = RSA.Create();

                rsa.ImportFromPem(publicKeyPem);

                // Convert message and signature
                byte[] messageBytes = Convert.FromBase64String(data.header.Nonce);
                byte[] signatureBytes = ByteUtils.FromBase64Url(data.header.DPoP);

                // Verify signature
                bool isValid = rsa.VerifyData(
                    messageBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                    //RSASignaturePadding.Pss
                );

                if (!isValid)
                {
                    _logger.LogError("Unauthorized. Replay detected");

                    return Unauthorized(new { status = "invalid", error = "Replay detected" });
                }

                // Decrypt symmetric key
                var symmetricKey = DecryptSymmetricKey(data.header.EncryptedKey);

                // Read remaining payload
                var encryptedPayload = data.payload; //not really encrypted, to change later

                // Decrypt payload using symmetric key
                var decryptedPayload = DecryptPayload(encryptedPayload, symmetricKey);

                var request = JsonSerializer.Deserialize<EnrollmentRequest>(
                 decryptedPayload,
                 new JsonSerializerOptions
                 {
                     PropertyNameCaseInsensitive = true
                 });

                EnrollmentResponse
                    responses = await _registrationActivityService.fetchEnrollment(data.header.ActivityCode, deviceProfile.TenantId, deviceProfile.DeviceId, request);

                return Ok(responses);

            }
            catch (SqlException ex) when (ex.Number == 50020)
            {
                _logger.LogError(ex, "Device unauthorized to download data for this beneficiary.");

                return Unauthorized(new
                {

                    success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (SqlException ex) when (ex.Number == 50010)
            {
                _logger.LogError(ex, "Beneficiary not enrolled in distribution.");
                
                return NotFound(new
                {

                    success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while performing enrollment.");

                return BadRequest(new
                {

                    success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }

        }




        [HttpPost("verify/templates")]
        public async Task<IActionResult> VerifyTemplates([FromBody] VerifyTemplatesPacket data)
        {
            try
            {
                _logger.LogInformation("Receiving Verification request..");


                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

                //return Ok(new List<BiometricVerificationResponse>());
                var result = await _contextValidator.ValidateAsync(User, ip);
                if (!result.Ok)
                    return StatusCode(result.StatusCode, new { error = result.Error });

                DeviceProfile deviceProfile = _contextValidator.DeviceProfile();

                // Load the public key from database
                byte[] derBytes = await _pubService.getClientPublickey(deviceProfile.DeviceId); // PEM format

                string publicKeyPem = PemHelper.DerBytesB64(derBytes);

                using RSA rsa = RSA.Create();

                rsa.ImportFromPem(publicKeyPem);

                // Convert message and signature
                byte[] messageBytes = Convert.FromBase64String(data.header.Nonce);
                byte[] signatureBytes = ByteUtils.FromBase64Url(data.header.DPoP);

                // Verify signature
                bool isValid = rsa.VerifyData(
                    messageBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                    //RSASignaturePadding.Pss
                );

                if (!isValid)
                {
                    _logger.LogError("Unauthorized. Replay detected");

                    return Unauthorized(new { status = "invalid", error = "Replay detected" });
                }

                // Decrypt symmetric key
                var symmetricKey = DecryptSymmetricKey(data.header.EncryptedKey);

                // Read remaining payload
                var encryptedPayload = data.templates; //not really encrypted, to change later

                // Decrypt payload using symmetric key
                var decryptedPayload = DecryptPayload(encryptedPayload, symmetricKey);

                var templates = JsonSerializer.Deserialize<List<BiometricVerificationRequest>>(
                 decryptedPayload,
                 new JsonSerializerOptions
                 {
                     PropertyNameCaseInsensitive = true
                 });

                Guid jobId = new Guid(data.header.BatchId);

                VerificationExtra v = new VerificationExtra();

                if(!string.IsNullOrEmpty(data.header.Extra))
                    v = GetVerificationExtra(data.header.Extra);

                List<BiometricVerificationResponse>
                    responses = await _registrationActivityService.VerifyTemplates(deviceProfile.TenantId, jobId, data.header.ActivityCode, deviceProfile.DeviceId, v.DistributionId, templates);

                _logger.LogInformation("Sending job {JobId} for processing.", jobId);

                _bus.SendMessageAsync(jobId.ToString());

                return Ok( responses );
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while Verifying Templates");

                return BadRequest(new
                {

                    success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }

        }

        private VerificationExtra GetVerificationExtra(string extra)
        {
            try
            {
                return JsonSerializer.Deserialize<VerificationExtra>(extra);
            }
            catch 
            {
                return new VerificationExtra();
            }
        }

        [HttpPost("push")]
        [Consumes("application/x-ndjson")]
        public async Task<IActionResult> PostRegistration(/*HttpRequest request*/)
        {

            try
            {
                _logger.LogInformation("Receiving registration data..");

                // Allow the body to be read multiple times if you need it later
                HttpRequest request = Request;
                request.EnableBuffering();

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

                var result = await _contextValidator.ValidateAsync(User, ip);
                if (!result.Ok)
                    return StatusCode(result.StatusCode, new { error = result.Error });

                DeviceProfile deviceProfile = _contextValidator.DeviceProfile();


                // Read header chunk (first line of stream)
                // using var reader = new StreamReader(Request.Body);
                using var reader = new StreamReader(request.Body, Encoding.UTF8);
                var headerLine = await reader.ReadLineAsync();
                var header = JsonSerializer.Deserialize<DataPacketHeader>(headerLine);

                // Load the public key from database
                byte[] derBytes = await _pubService.getClientPublickey(deviceProfile.DeviceId); // PEM format

                string publicKeyPem = PemHelper.DerBytesB64(derBytes);

                using RSA rsa = RSA.Create();

                rsa.ImportFromPem(publicKeyPem);


                // Convert message and signature
                byte[] messageBytes = Convert.FromBase64String(header.Nonce);
                byte[] signatureBytes = ByteUtils.FromBase64Url(header.DPoP);

                // Verify signature
                bool isValid = rsa.VerifyData(
                    messageBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                    //RSASignaturePadding.Pss
                );

                if (!isValid)
                {
                    return Unauthorized(new { status = "invalid", error = "Replay detected" });
                }

                // Decrypt symmetric key
                var symmetricKey = DecryptSymmetricKey(header.EncryptedKey);

                // Read remaining payload
                var encryptedPayload = await reader.ReadToEndAsync(); //not really encrypted, to change later

                Guid jobId = Guid.NewGuid();

                // Decrypt payload using symmetric key
                var decryptedPayload = DecryptPayload(encryptedPayload, symmetricKey);

                // Stream decrypted data into staging
                using var payloadStream = new MemoryStream(Encoding.UTF8.GetBytes(decryptedPayload));

                if (await ProcessStreamToStagingAsync(deviceProfile.DeviceId, deviceProfile.TenantId, header.ActivityCode, jobId, payloadStream))
                {
                    _logger.LogInformation("Enqueue background load using DataLoaderService..");
                    // enqueue background load using IDataLoaderService
                    _taskQueue.QueueBackgroundWorkItem(async ct =>
                    {
                        try
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var loader = scope.ServiceProvider.GetRequiredService<IDataLoaderService>();

                            loader.Load(jobId); // sync call, wrapped in async work item
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error running IDataLoaderService. Load for job {JobId}", jobId);
                        }

                        await Task.CompletedTask;
                    });


                    return Accepted(new
                    {
                        Success = true,
                        BatchId = header.Nonce
                    });

                }
                else
                {
                    _logger.LogError("Failed to save data to staging. Job {JobId}", jobId);
                    
                    return BadRequest(new
                    {
                        success = false,
                        BatchId = header?.Nonce,
                        ErrorMessage = "Batch processing failed due to an unexpected error.",
                        Timestamp = DateTime.UtcNow
                    });
                }

            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error Pushing data");

                return BadRequest(new
                {
                    success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }

        }
        

        private string DecryptPayload(string encryptedPayload, string symmetricKey)
        {
            return encryptedPayload;
        }

        private string DecryptSymmetricKey(string encryptedKey)
        {
            return encryptedKey;
        }

        private async Task<bool> ProcessStreamToStagingAsync(string deviceId, int tenantd, string activityCode, Guid batchId, Stream payloadStream)
        {
            _logger.LogInformation("Adding data to staging..");

            using var reader = new StreamReader(payloadStream);
            string? line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    // Parse JSON line
                    using var doc = JsonDocument.Parse(line);
                    var type = doc.RootElement.GetProperty("type").GetString();
                    var payload = doc.RootElement.GetProperty("payload").GetRawText();

                    StagingType _type = StagingType.household;

                    if (Enum.TryParse<StagingType>(type, true, out var stage))
                    {
                        _type = stage;
                    }

                    // Create staging record
                    var stagingRecord = new RegistrationActivityStaging
                    {
                        DeviceId = deviceId,
                        TenantId = tenantd,
                        ActivityCode = activityCode,
                        BatchId = batchId,
                        Type = _type,
                        Payload = payload
                    };

                    // Save to DB
                    await _registrationActivityService.SaveAsync(stagingRecord);
                }
                catch (Exception ex)
                {
                    // Log error and continue (or handle according to your retry policy)
                    _logger.LogError(ex, $"Failed to process line: {line}");
                    return false;
                }
            }

            return true;
        }
    }
}
