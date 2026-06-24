using BRaVe_Mobile_Backend.DTOs;
using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;
using BRaVe_Mobile_Backend.Models;
using BRaVe_Mobile_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace BRaVe_Mobile_Backend.Controllers.devices_onboarding
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly JwsSignerService _jwsSignerService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IPubService _pubService;
        private readonly ILogger<EnrollClaimController> _logger;

        public TokenController(JwsSignerService jwsSignerService, IRefreshTokenService refreshTokenService, IPubService pubService, ILogger<EnrollClaimController> logger)
        {
            _jwsSignerService = jwsSignerService;
            _refreshTokenService = refreshTokenService;
            _pubService = pubService;
            _logger = logger;
        }


        [HttpPost("refresh")]
        public async Task<ActionResult<TokenRefreshResponse>> refresh(TokenRefreshRequest request)
        {
            try
            {

                // Load the public key (could be from config, database, etc.)
                byte[] derBytes = await _pubService.getClientPublickey(request.DeviceId); // PEM format

                string publicKeyPem = PemHelper.DerBytesB64(derBytes);

                using RSA rsa = RSA.Create();

                rsa.ImportFromPem(publicKeyPem);


                // Convert message and signature
                byte[] messageBytes = Convert.FromBase64String(request.Nonce);
                //ByteUtils.FromBase64Url(request.Nonce);//Encoding.UTF8.GetBytes(request.Nonce);
                byte[] signatureBytes = /*Convert.FromBase64String(request.DPoP);*/ 
                    ByteUtils.FromBase64Url(request.DPoP);

                // Verify signature
                bool isValid = rsa.VerifyData(
                    messageBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    //RSASignaturePadding.Pkcs1
                    RSASignaturePadding.Pss // ← use PSS instead of PKCS#1 v1.5
                );

                if (isValid)
                {
                    var result = RefreshFlow(request);

                    TokenRefreshResponse response = new TokenRefreshResponse();
                    response.Refresh = result.Refresh;
                    response.Jwt = _jwsSignerService.Issue(result.Tenant, request.DeviceId);

                    return Ok(response);
                }

            }
            catch (Exception ex) { 
            
            
            }
            

            return Unauthorized(new { status = "invalid signature" });
        }

        private RefreshTokenTenantPair RefreshFlow(TokenRefreshRequest req)
        {

            // 1) Hash incoming refresh token
            byte[] tokenBytes = ByteUtils.FromBase64Url(req.TokenRefresh); // same format you minted earlier
            byte[] tokenHash = SHA256.HashData(tokenBytes);

            // 2) Pre-mint new refresh (raw + hash)
            Span<byte> rnd = stackalloc byte[32];
            RandomNumberGenerator.Fill(rnd);
            string newRaw = ByteUtils.ToBase64Url(rnd);
            byte[] newHash = SHA256.HashData(ByteUtils.FromBase64Url(newRaw));

            var result = _refreshTokenService.RefreshAsync(new TokenRefreshDto
            {
                DeviceId = req.DeviceId,    
                Enumerator = req.Enumerator,
                TokenHash = tokenHash,
                FreshToken = newHash

            }).Result;

            return result;
        }

    }
}
