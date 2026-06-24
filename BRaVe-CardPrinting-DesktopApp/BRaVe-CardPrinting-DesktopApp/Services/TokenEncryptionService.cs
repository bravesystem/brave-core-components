using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BRaVe_CardPrinting_DesktopApp.Security
{
    public class TokenEncryptionService
    {
        private readonly byte[] _key;

        public TokenEncryptionService(byte[] key)
        {
            _key = key;
        }

        // ==========================================
        // ENCRYPT
        // ==========================================
        public string Encrypt(string plainText)
        {
            using var aes = Aes.Create();

            aes.Key = _key;

            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();

            var plainBytes = Encoding.UTF8.GetBytes(plainText);

            var cipherBytes = encryptor.TransformFinalBlock(
                plainBytes,
                0,
                plainBytes.Length);

            // prepend IV to ciphertext
            var combined = aes.IV.Concat(cipherBytes).ToArray();

            return Convert.ToBase64String(combined);
        }

        // ==========================================
        // DECRYPT
        // ==========================================
        public string Decrypt(string cipherText)
        {
            var combined = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();

            aes.Key = _key;

            // first 16 bytes = IV
            var iv = new byte[16];

            Array.Copy(combined, 0, iv, 0, 16);

            aes.IV = iv;

            // remaining bytes = encrypted token
            var cipherBytes = new byte[combined.Length - 16];

            Array.Copy(
                combined,
                16,
                cipherBytes,
                0,
                cipherBytes.Length);

            using var decryptor = aes.CreateDecryptor();

            var plainBytes = decryptor.TransformFinalBlock(
                cipherBytes,
                0,
                cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}