namespace BRaVe_Mobile_Backend.Helpers
{
    public static class Thumbprint
    {
        /*public static bool TryParse(string s, out byte[] bytes)
        {

            bytes = Array.Empty<byte>();

            if (string.IsNullOrWhiteSpace(s))
                return false;

            // Check length multiple of 4
            if (s.Length % 4 != 0)
                return false;

            // Check valid Base64 characters
            foreach (char c in s)
            {
                if (!(char.IsLetterOrDigit(c) || c == '+' || c == '/' || c == '='))
                    return false;
            }

            // Try decoding
            try
            {
                bytes = Convert.FromBase64String(s);
                return true;
            }
            catch
            {
                return false;
            }

        }*/


        public static byte[] PemToDer(string pemString)
        {
            if (string.IsNullOrWhiteSpace(pemString))
                throw new ArgumentException("PEM is empty.", nameof(pemString));


            // Strip headers and decode Base64
            string base64Body = pemString
                .Replace("-----BEGIN PUBLIC KEY-----", "")
                .Replace("-----END PUBLIC KEY-----", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Trim();

            byte[] derBytes = Convert.FromBase64String(base64Body);

            return derBytes;    

        }

    }
}
