using System.Security.Claims;
using Serilog;

namespace BRaVe_Management_Backend.Helpers
{
    public class EmailClaimHelper
    {
        public static string? ResolveEmail(ClaimsPrincipal user, IReadOnlyList<string> priorities)
        {
            try
            {
                Log.Information("Attempting to resolve email from ClaimsPrincipal with {ClaimCount} claims", user?.Claims.Count() ?? 0);

                // Try well-known types first
                var email = user.FindFirstValue(ClaimTypes.Email);

                if (!string.IsNullOrWhiteSpace(email))
                {
                    Log.Information("Resolved email using ClaimTypes.Email: {Email}", email);
                    return email;
                }

                // Try custom priorities
                foreach (var claimType in priorities)
                {
                    var value = user.FindFirstValue(claimType);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        Log.Information("Resolved email using priority claim type {ClaimType}: {Email}", claimType, value);
                        return value;
                    }
                }

                Log.Warning("Failed to resolve email from claims — returning null");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while resolving email from claims");
                throw;
            }
        }
    }
}
