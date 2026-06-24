using Azure.Core;
using BRaVe_Mobile_Backend.DTOs;
using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;
using BRaVe_Mobile_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace BRaVe_Mobile_Backend.Controllers.server_operations
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class EnumeratorsController : ControllerBase
    {
        private readonly IEnumeratorService _enumeratorService;
        private readonly IRequestContextValidator _contextValidator;
        private readonly ILogger<EnumeratorsController> _logger;
        private readonly IPubService _pubService;
        public EnumeratorsController(IPubService pubService, IRequestContextValidator contextValidator, IEnumeratorService enumeratorService, ILogger<EnumeratorsController> logger)
        {
            _pubService = pubService;
            _contextValidator = contextValidator;
            _enumeratorService = enumeratorService;
            _logger = logger;

        }


        [HttpGet("refresh/{Flag}")]
        public async Task<ActionResult<List<Enumerator>>> Refresh(int Flag)
        {
            try
            {

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();


                var result = await _contextValidator.ValidateAsync(User, ip);
                if (!result.Ok)
                    return StatusCode(result.StatusCode, new { error = result.Error });

                DeviceProfile deviceProfile = _contextValidator.DeviceProfile();

                List<Enumerator> list = await _enumeratorService.GetAll(deviceProfile.DeviceId, deviceProfile.TenantId, ip, Flag);

                return list;
            }
            catch (Exception ex)
            {

            }

            return NotFound(new
            {
                status = "invalid",
                error = $"Cannot download Enumerator list"
            });
        }

        /*[HttpPost("setpin")]
        public async Task<ActionResult<List<Enumerator>>> SetPin(SetPinRequestDto dto)
        {
            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

                var result = await _contextValidator.ValidateAsync(User, ip);
                if (!result.Ok)
                    return StatusCode(result.StatusCode, new { error = result.Error });

                DeviceProfile deviceProfile = _contextValidator.DeviceProfile();

                // Load the public key (could be from config, database, etc.)
                byte[] derBytes = await _pubService.getClientPublickey(deviceProfile.DeviceId); // PEM format

                string publicKeyPem = PemHelper.DerBytesB64(derBytes);

                using RSA rsa = RSA.Create();

                rsa.ImportFromPem(publicKeyPem);


                SetPinRequest data = new SetPinRequest();

                data.DeviceId = deviceProfile.DeviceId;
                data.RequestIp = ip;
                data.TenantId = deviceProfile.TenantId;
                data.Code = dto.code;
                data.IsDefault = "0000"==dto.oldPin;
                data.OldPin = data.IsDefault ? Array.Empty<byte>() : Crypto.Sha256Bytes(dto.oldPin);
                data.NewPin = Crypto.Sha256Bytes(dto.newPin);

                List<Enumerator> list = await _enumeratorService.SetPin(data);

                return list;
            }
            catch (Exception ex)
            {

            }

            return NotFound(new
            {
                status = "invalid",
                error = $"Cannot download Enumerator list"
            });
        }
        */

        private async Task<(bool Ok, ActionResult<List<Enumerator>>? ErrorResult, DeviceProfile? Profile, string? ClientIp)> PrepareContextAsync()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _contextValidator.ValidateAsync(User, ip);
            if (!result.Ok)
            {
                return (false, StatusCode(result.StatusCode, new { error = result.Error }), null, ip);
            }

            var deviceProfile = _contextValidator.DeviceProfile();
            if (deviceProfile is null)
            {
                return (false, StatusCode(StatusCodes.Status400BadRequest, new { error = "Invalid device profile" }), null, ip);
            }

            return (true, null, deviceProfile, ip);
        }

        [HttpPost("setpin")]
        public async Task<ActionResult<List<Enumerator>>> SetPin([FromBody] SetPinRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(dto.code) || string.IsNullOrEmpty(dto.newPin))
                return BadRequest(new { error = "Code and newPin are required." });

            try
            {
                var prep = await PrepareContextAsync();
                if (!prep.Ok)
                    return prep.ErrorResult!;

                var deviceProfile = prep.Profile!;
                var ip = prep.ClientIp;

                // If you actually need the public key, keep and use it. Otherwise remove block.
                // byte[] derBytes = await _pubService.GetClientPublicKeyAsync(deviceProfile.DeviceId);
                // string publicKeyPem = PemHelper.DerBytesB64(derBytes);
                // using RSA rsa = RSA.Create();
                // rsa.ImportFromPem(publicKeyPem);

                var data = new SetPinRequest
                {
                    DeviceId = deviceProfile.DeviceId,
                    RequestIp = ip,
                    TenantId = deviceProfile.TenantId,
                    Code = dto.code,
                    //IsDefault = string.Equals(dto.oldPin, "0000", StringComparison.Ordinal), // consider config-driven
                    OldPin = Crypto.Sha256Bytes(dto.oldPin), // prefer slow, salted KDF
                    NewPin = Crypto.Sha256Bytes(dto.newPin)
                };

                var list = await _enumeratorService.SetPin(data);
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SetPin for code {Code}", dto.code);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = "error",
                    error = "Failed to set PIN"
                });
            }
        }

        [HttpPost("extendexpiry")]
        public async Task<ActionResult<List<Enumerator>>> ExtendExpiry([FromBody] ExtendAccessDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(dto.code) || string.IsNullOrEmpty(dto.pin))
                return BadRequest(new { error = "Code and pin are required." });

            try
            {
                var prep = await PrepareContextAsync();
                if (!prep.Ok) return prep.ErrorResult!;
                var deviceProfile = prep.Profile!;
                var ip = prep.ClientIp;

                // Optional public key usage removed; re-add if needed.

                var pinHash = Crypto.Sha256Bytes(dto.pin);

                var data = new SetPinRequest
                {
                    DeviceId = deviceProfile.DeviceId,
                    RequestIp = ip,
                    TenantId = deviceProfile.TenantId,
                    Code = dto.code,
                    OldPin = pinHash, // verify current PIN
                    NewPin = pinHash  // no change; service should interpret as "renew access"
                };

                var list = await _enumeratorService.SetPin(data);
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExtendAccess for code {Code}", dto.code);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = "error",
                    error = "Failed to extend access"
                });
            }
        }
    }
}
