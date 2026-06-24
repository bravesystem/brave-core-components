using BRaVe_Mobile_Backend.DTOs;
using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace BRaVe_Mobile_Backend.Controllers.devices_onboarding
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class IntegrityController : ControllerBase
    {
        INonceService _nonceService;
        public IntegrityController(INonceService nonceService)
        {
            _nonceService = nonceService;
        }


        [HttpPost("verify")]
        public async Task<IntegrityTokenResponse> VerifyToken([FromBody] IntegrityTokenRequest request)
        {
            try
            {

                if (request is null ||
                    string.IsNullOrWhiteSpace(request.Token))
                    return new IntegrityTokenResponse()
                    {
                        success = false,
                        error = "Missing fields.",
                    };

                // 1) Validate integrity token with Google
                /*GoogleJsonWebSignature.Payload payload;
                try
                {
                    payload = await GoogleJsonWebSignature.ValidateAsync(
                        request.Token,
                        new GoogleJsonWebSignature.ValidationSettings
                        {
                            // You can restrict audience, etc. if applicable

                        });
                }
                catch (Exception ex)
                {
                    return new IntegrityTokenResponse
                    {
                        success = false,
                        error = $"Token invalid: {ex.Message}"
                    };
                }*/


                // 2) Compare nonce in token with expectedNonce from our store
                //var nonceBytes = ByteUtils.FromBase64Url(payload.Nonce);
                var nonceBytes = ByteUtils.FromBase64Url(request.Token);

                string? UsedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();

                if (await _nonceService.validateNonce(request.Id, SHA256.HashData(nonceBytes), UsedByIp))
                {

                    return new IntegrityTokenResponse()
                    {
                        success = true,
                        error = "",
                    };

                }

                return new IntegrityTokenResponse
                {
                    success = false,
                    error = "Nounce mismatch"
                };

            }
            catch (Exception ex)
            {
                return new IntegrityTokenResponse
                {
                    success = false,
                    error = ex.Message
                };
            }
        }

    }
}
