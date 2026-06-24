using System.Text;
using System.Text.Json;

namespace BRaVe_Portal.Helpers
{
    public static class JwtPeek
    {
        // Quick-and-safe base64url decode + parse 'iss' from JWT payload without validating
        public static string? TryGetIssuer(string jwt)
        {
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length < 2) return null;
                var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
                using var doc = JsonDocument.Parse(payloadJson);
                return doc.RootElement.TryGetProperty("iss", out var issProp) ? issProp.GetString() : null;
            }
            catch { return null; }
        }

        private static byte[] Base64UrlDecode(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }
    }
}
