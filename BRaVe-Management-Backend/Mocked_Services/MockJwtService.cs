using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Mocked_Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BRaVe_Management_Backend.Mocked_Services
{
    public class MockJwtService : IJwtService
    {
        private readonly IOptionsMonitor<MockJwtOptions> _opts;
        private readonly IRoleService _roles;
        private readonly ILogger<MockJwtService> _logger;

        public MockJwtService(IOptionsMonitor<MockJwtOptions> opts, IRoleService roles, ILogger<MockJwtService> logger)
        {
            _opts = opts;
            _roles = roles;
            _logger = logger;
        }

        public async Task<string> CreateTokenAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Starting JWT token generation...");

            try
            {
                var opt = _opts.CurrentValue;
                var p = opt.UserProfile;
                var email = p.emails?.FirstOrDefault();

                _logger.LogInformation("Generating JWT for user: {Email}", email ?? "(unknown)");

                var claims = new List<Claim>
                {
                    new(JwtRegisteredClaimNames.Sub, p.sub),
                    new("oid", p.oid),
                    new("tid", p.tid),
                    new("preferred_username", p.preferred_username),
                    new(ClaimTypes.Email, email ?? string.Empty),
                    new(JwtRegisteredClaimNames.Name, p.name),
                    new("given_name", p.given_name),
                    new("family_name", p.family_name),
                    new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
                };

                foreach (var e in p.emails ?? Enumerable.Empty<string>())
                    claims.Add(new("emails", e));

                foreach (var kv in p.extra ?? new())
                    claims.Add(new(kv.Key, kv.Value));

                // Uncomment if roles should be fetched
                /*
                var roles = await _roles.GetRolesAsync(email, ct);
                foreach (var r in roles)
                    claims.Add(new(ClaimTypes.Role, r));
                _logger.LogInformation("Added {Count} roles for user {Email}", roles.Count(), email);
                */

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opt.SigningKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var now = DateTime.UtcNow;

                var token = new JwtSecurityToken(
                    issuer: opt.Issuer,
                    audience: opt.Audience,
                    claims: claims,
                    notBefore: now,
                    expires: now.AddMinutes(opt.AccessTokenMinutes),
                    signingCredentials: creds);

                var jwt = new JwtSecurityTokenHandler().WriteToken(token);

                _logger.LogInformation("JWT successfully created for {Email}. Expires at {Expiry}.", email, token.ValidTo);

                return jwt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token.");
                throw;
            }
        }
    }
}
