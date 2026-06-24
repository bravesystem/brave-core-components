namespace BRaVe_Mobile_Backend.Helpers
{
    public class ByteUtils
    {
        public static byte[] FromBase64Url(string s)
        {
            string b64 = s.Replace('-', '+').Replace('_', '/');
            switch (b64.Length % 4) { case 2: b64 += "=="; break; case 3: b64 += "="; break; }
            return Convert.FromBase64String(b64);
        }
        public static string ToBase64Url(ReadOnlySpan<byte> bytes)
        {
            return Convert.ToBase64String(bytes.ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

       /* public static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }*/
    }
}
