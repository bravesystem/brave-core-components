using BRaVe_Mobile_Backend.DTOs;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace BRaVe_Mobile_Backend.Models
{
    public sealed class EnrollmentJwsValidator
    {
        private readonly TokenValidationParameters _p;
        private readonly JwtSecurityTokenHandler _h = new();

        public EnrollmentJwsValidator(RsaSecurityKey signingKey)
        {
            _p = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://auth.brave.iom.int",
                ValidateAudience = true,
                ValidAudience = "brave-enroll",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(10)
            };
        }

        public JwtSecurityToken Validate(string jws)
        {
            try
            {
                _h.ValidateToken(jws, _p, out var principal);
                return (JwtSecurityToken)((JwtSecurityTokenHandler)_h).ReadToken(jws)!;
            }
            catch (SecurityTokenExpiredException) { throw new ActivateErrors.JwsExpired(); }
            catch { throw new ActivateErrors.InvalidJws(); }
        }
    }
}
