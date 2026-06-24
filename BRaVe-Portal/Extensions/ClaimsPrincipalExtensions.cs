using System.Net.Mail;
using System.Security.Claims;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace BRaVe_Portal.Extensions
{
    public static class ClaimsPrincipalExtensions
    {

        public static string? GetUserEmailLike(this ClaimsPrincipal principal)
        {
            return
                // 1. Standard email claim (v2 often has this)
                principal.FindFirst(ClaimTypes.Email)?.Value
                ?? principal.FindFirst("email")?.Value

                // 2. v2 “preferred username” (often an email)
                ?? principal.FindFirst("preferred_username")?.Value

                // 3. UPN (your current case)
                ?? principal.FindFirst(ClaimTypes.Upn)?.Value
                ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn")?.Value;
        }


        public static string? Identifier(this ClaimsPrincipal principal)
        {
            if (principal == null) return null;

            // 1. OID when mapping is OFF (plain "oid")
            var oid = principal.FindFirst("oid")?.Value;
            if (!string.IsNullOrEmpty(oid))
                return oid;

            // 2. OID when mapping is ON (objectidentifier)
            oid = principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            if (!string.IsNullOrEmpty(oid))
                return oid;

            // 3. Fallback: NameIdentifier (sub) – not ideal, but unique per app+tenant
            var sub = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? principal.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(sub))
                return sub;

            // 4. Final fallback: email-like identifiers (UPN / email / preferred_username)
            var emailLike =
                principal.FindFirst(ClaimTypes.Email)?.Value
                ?? principal.FindFirst("email")?.Value
                ?? principal.FindFirst(ClaimTypes.Upn)?.Value
                ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn")?.Value
                ?? principal.FindFirst("preferred_username")?.Value;

            return emailLike;
        }

        public static string? DisplayName(this ClaimsPrincipal principal)
        {
            if (principal == null) return null;

            // 1. Standard "name" claims (most common)
            var name =
                principal.FindFirst("name")?.Value
                ?? principal.FindFirst(ClaimTypes.Name)?.Value
                ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;

            if (!string.IsNullOrWhiteSpace(name))
                return name;

            // 2. Given name + surname (when provided separately)
            var givenName =
                principal.FindFirst(ClaimTypes.GivenName)?.Value
                ?? principal.FindFirst("given_name")?.Value
                ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname")?.Value;

            var surname =
                principal.FindFirst(ClaimTypes.Surname)?.Value
                ?? principal.FindFirst("family_name")?.Value
                ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname")?.Value;

            if (!string.IsNullOrWhiteSpace(givenName) || !string.IsNullOrWhiteSpace(surname))
            {
                return $"{givenName} {surname}".Trim();
            }

            // 3. Fallback to something email-like if no proper name
            var emailLike =
                principal.FindFirst(ClaimTypes.Email)?.Value
                ?? principal.FindFirst("email")?.Value
                ?? principal.FindFirst(ClaimTypes.Upn)?.Value
                ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn")?.Value
                ?? principal.FindFirst("preferred_username")?.Value;

            if (!string.IsNullOrWhiteSpace(emailLike))
                return emailLike;

            // 4. Last resort: subject / nameidentifier
            var sub =
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst("sub")?.Value;

            return sub;
        }


        public static int Tenant(this ClaimsPrincipal user) =>
                int.TryParse(user.FindFirstValue("Tenant"), out var id) ? id : 0;


        public static string Mission(this ClaimsPrincipal user) => user.FindFirstValue("Mission");

        public static string  AllRoles(this ClaimsPrincipal user) => user.FindFirstValue("AllRoles");

    }
}
