namespace BRaVe_Management_Backend.Helpers
{
    public static class Thumbprint
    {
        public static bool TryParse(string s, out byte[] bytes)
        {
            // accept base64url(32) or hex(64)
            try
            {
                if (s.Length == 43 || s.Length == 44)
                { // b64url(32)
                    bytes = Convert.FromBase64String(s.Replace('-', '+').Replace('_', '/').PadRight(44, '='));
                    return bytes.Length == 32;
                }
                if (s.Length == 64)
                { // hex
                    bytes = Enumerable.Range(0, 32).Select(i => Convert.ToByte(s.Substring(i * 2, 2), 16)).ToArray();
                    return true;
                }
            }
            catch { }
            bytes = Array.Empty<byte>();
            return false;
        }
    }
}
