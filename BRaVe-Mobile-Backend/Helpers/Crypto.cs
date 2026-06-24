using System.Security.Cryptography;
using System.Text;

namespace BRaVe_Mobile_Backend.Helpers
{
    public static class Crypto
    {

        // --------- Legacy SHA-256 (if you already have it) ---------
        public static byte[] Sha256Bytes(string s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        }

        // --------- Configuration (tune for your environment) ----------
        // Aim for ~100–300ms per hash on your server hardware.
        public const int DefaultPbkdf2Iterations = 200_000;    // adjust based on perf
        public const int DefaultSaltSizeBytes = 16;            // 128-bit salt
        public const int DefaultHashSizeBytes = 32;            // 256-bit output

        // Optional pepper (application-level secret). Store it in KeyVault/Secret Manager, not in code.
        // If you use a pepper, version it and handle rotation carefully.
        private static readonly byte[]? Pepper = null; // or Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("PIN_PEPPER") ?? "");

        // --------- PBKDF2: Hashing ---------
        public static (byte[] Hash, byte[] Salt, int Iterations) Pbkdf2Hash(
            string secret,
            byte[]? salt = null,
            int iterations = DefaultPbkdf2Iterations,
            int hashSizeBytes = DefaultHashSizeBytes)
        {
            if (string.IsNullOrEmpty(secret))
                throw new ArgumentException("Secret must not be null/empty.", nameof(secret));

            salt ??= RandomBytes(DefaultSaltSizeBytes);

            // Combine secret + pepper (if any) before KDF
            var secretBytes = Encoding.UTF8.GetBytes(secret);
            byte[] material = Pepper is { Length: > 0 }
                ? Combine(secretBytes, Pepper)
                : secretBytes;

            using var pbkdf2 = new Rfc2898DeriveBytes(material, salt, iterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(hashSizeBytes);
            return (hash, salt, iterations);
        }

        // --------- PBKDF2: Verification ---------
        public static bool Pbkdf2Verify(string secret, byte[] expectedHash, byte[] salt, int iterations, int hashSizeBytes = DefaultHashSizeBytes)
        {
            var (hash, _, _) = Pbkdf2Hash(secret, salt, iterations, hashSizeBytes);
            return FixedTimeEquals(hash, expectedHash);
        }
       

        // Optional: Salted SHA-256 (not recommended for new usage; PBKDF2 is better)
        public static byte[] Sha256Bytes(byte[] input) // overload used in your existing code
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(input);
        }

        // --------- Utilities ---------
        public static byte[] RandomBytes(int length)
        {
            var bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            return bytes;
        }

        private static bool FixedTimeEquals(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static byte[] Combine(byte[] a, byte[] b)
        {
            var result = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, result, 0, a.Length);
            Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
            return result;
        }

        // Convenience wrappers if you prefer single-call APIs:

        public static byte[] Pbkdf2Bytes(string secret) =>
            Pbkdf2Hash(secret).Hash;

        public static string Pbkdf2Base64(string secret)
        {
            var (hash, salt, iter) = Pbkdf2Hash(secret);
            // Example stored format: pbkdf2_sha256$iter$saltB64$hashB64
            return $"pbkdf2_sha256${iter}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool Pbkdf2VerifyFromBase64(string secret, string stored)
        {
            // Expect format: pbkdf2_sha256$iterations$saltB64$hashB64
            var parts = stored.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4 || !parts[0].Equals("pbkdf2_sha256", StringComparison.OrdinalIgnoreCase))
                throw new FormatException("Invalid PBKDF2 stored format.");

            int iterations = int.Parse(parts[1]);
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            return Pbkdf2Verify(secret, expectedHash, salt, iterations, expectedHash.Length);
        }

    }
}
