using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace BRaVe_Mobile_Backend.Controllers.devices_onboarding
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class NonceController : ControllerBase
    {
        INonceService _nonceService;
        public NonceController(INonceService nonceService) 
        { 
            _nonceService = nonceService;
        }    


        [HttpGet("generate/{deviceId}")]
        public IActionResult GenerateNonce( string deviceId)
        {
            // Generate a secure random nonce
            var nonce = NonceUtil.Generate();
                //GenerateSecureNonce();

            Guid nonceId = Guid.NewGuid();

            byte[] nonceBytes = Encoding.ASCII.GetBytes(nonce);

            _nonceService.saveNonce(nonceId, deviceId, SHA256.HashData(nonceBytes), "Play-Integrity", TimeSpan.FromMinutes(5), null);

            string noncebase64 = Convert.ToBase64String(nonceBytes);

            return Ok( new { nonceId, nonce = noncebase64 } );
        }

    }
}
