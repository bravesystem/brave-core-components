using System.Net.Mail;
using System.Security.Claims;

namespace BRaVe_Portal.Extensions
{
    public static class ClaimsPrincipalEmailListExtensions
    {
        /*
        // Common claim types that may carry an email
        private static readonly string[] EmailClaimTypes =
        {
            ClaimTypes.Email,     // WS-Fed
            "email",              // OIDC
            "preferred_username", // often email-like in Entra ID
            "upn",                // may be email-like
            "mail"                // AAD/Graph frequently uses 'mail'
        };

        /// <summary>
        /// Returns all distinct email addresses found in the user's claims.
        /// If requireVerified=true, only returns when email_verified=true is present.
        /// </summary>
        public static IEnumerable<string> Emails(this ClaimsPrincipal user, bool requireVerified = false)
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool verified = string.Equals(
                user.FindFirst("email_verified")?.Value, "true",
                StringComparison.OrdinalIgnoreCase);

            if (requireVerified && !verified)
                return Enumerable.Empty<string>();

            // Multi-valued "emails" claim(s)
            foreach (var c in user.FindAll("emails"))
                AddPossiblyDelimited(results, c.Value);

            // Single-value email-ish claims
            foreach (var t in EmailClaimTypes)
            {
                var v = user.FindFirstValue(t);
                if (!string.IsNullOrWhiteSpace(v))
                    AddPossiblyDelimited(results, v);
            }

            return results.Where(IsEmail);

            static void AddPossiblyDelimited(HashSet<string> set, string value)
            {
                // some providers cram multiple emails into one claim separated by , ; or spaces
                var parts = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(s => s.Trim());
                foreach (var p in parts)
                    set.Add(p);
            }

            static bool IsEmail(string s)
            {
                try { _ = new MailAddress(s); return true; }
                catch { return false; }
            }
        }

        /// <summary>
        /// Returns all emails joined by a semicolon (default "; ").
        /// </summary>
        public static string EmailsJoined(this ClaimsPrincipal user, bool requireVerified = false, string separator = "; ")
            => string.Join(separator, user.Emails(requireVerified));
    
        */
        }
        
}
