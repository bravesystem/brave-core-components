using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using Serilog;
using System.Security.Claims;

namespace BRaVe_Management_Backend.Helpers
{
    public class EmailClaimsNormalizer : IClaimsTransformation
    {
        private readonly IReadOnlyList<string> _priority;

        public EmailClaimsNormalizer(IOptions<AuthOptions> opts)
        {
            _priority = opts.Value.EmailClaimPriority ?? new List<string>();
            Log.Information("EmailClaimsNormalizer initialized with {PriorityCount} email claim priorities", _priority.Count);
        }

        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            try
            {
                Log.Information("Starting claims normalization for user with {ClaimCount} claims", principal.Claims.Count());

                var email = EmailClaimHelper.ResolveEmail(principal, _priority);
                if (string.IsNullOrWhiteSpace(email))
                {
                    Log.Warning("No valid email claim found during normalization");
                    return Task.FromResult(principal);
                }

                var id = (ClaimsIdentity)principal.Identity!;
                Log.Information("Resolved email: {Email}. Proceeding to normalize claims", email);

                // Ensure canonical Email claim
                if (!id.HasClaim(c => c.Type == ClaimTypes.Email))
                {
                    id.AddClaim(new Claim(ClaimTypes.Email, email));
                    Log.Information("Added missing ClaimTypes.Email claim: {Email}", email);
                }

                // Normalize Name claim
                if (id.Name != email)
                {
                    Log.Information("Updating Name claim from '{OldName}' to '{NewName}'", id.Name, email);
                    foreach (var c in id.FindAll(ClaimTypes.Name).ToList())
                        id.RemoveClaim(c);

                    id.AddClaim(new Claim(ClaimTypes.Name, email));
                    Log.Information("Replaced Name claim successfully");
                }

                Log.Information("Claims normalization completed for user {Email}", email);
                return Task.FromResult(principal);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while transforming claims in EmailClaimsNormalizer");
                throw;
            }
        }
    }

    public sealed class AuthOptions
    {
        public List<string> EmailClaimPriority { get; set; } = new();
    }
}
