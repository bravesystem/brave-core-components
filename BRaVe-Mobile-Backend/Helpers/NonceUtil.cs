namespace BRaVe_Mobile_Backend.Helpers
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    public static class NonceUtil
    {
        // Reuse one RNG instance
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        /// <summary>Generate a 32-byte random nonce and return base64url (no padding).</summary>
        public static string Generate() => Generate(32);

        /// <summary>Generate an N-byte random nonce and return base64url (no padding).</summary>
        public static string Generate(int length = 32)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }


        /// <summary>Generate raw random bytes.</summary>
        public static byte[] GenerateBytes(int numBytes)
        {
            if (numBytes <= 0) throw new ArgumentOutOfRangeException(nameof(numBytes));
            var buf = new byte[numBytes];
            Rng.GetBytes(buf);
            return buf;
        }

        /// <summary>base64url encode (no padding).</summary>
        public static string ToBase64Url(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        /// <summary>base64url decode.</summary>
        public static byte[] FromBase64Url(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) throw new ArgumentNullException(nameof(s));
            string b64 = s.Replace('-', '+').Replace('_', '/');
            switch (b64.Length % 4) { case 2: b64 += "=="; break; case 3: b64 += "="; break; }
            return Convert.FromBase64String(b64);
        }
    }

}
