using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using BRaVe_Biometric_Matching_Webjob.Interfaces;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using static BRaVe_Biometric_Matching_Webjob.Helpers.KeyVaultSecretNames;
using KeyVaultEncryptionAlgorithm = Azure.Security.KeyVault.Keys.Cryptography.EncryptionAlgorithm;

namespace BRaVe_Biometric_Matching_Webjob.Services
{
    /// <summary>
    /// Envelope encryption compatible with .NET Framework 4.7+.
    /// Requires NuGet: Azure.Core (includes Azure.Identity credentials from 1.53+), Azure.Security.KeyVault.Keys, BouncyCastle.Cryptography.
    /// Ensure TLS 1.2 is enabled at process startup when targeting .NET Framework.
    /// </summary>
    public sealed class AzureEncryptionService : IEncryptionService
    {
        private const byte FormatVersion = 1;
        private const int AesKeySizeBytes = 32;
        private const int NonceSizeBytes = 12;
        private const int TagSizeBytes = 16;
        private const int WrappedKeyLengthFieldSize = 4;

        private readonly CryptographyClient _cryptographyClient;

        public static bool IsLocal =>
                bool.TryParse(
                    Environment.GetEnvironmentVariable("RUNNING_IN_AZURE"),
                    out bool runningInAzure)
                && !runningInAzure;


        public AzureEncryptionService()
        {
            // Same sync pattern as SqlUatFormAccessService / Redis DI factory in Program.cs

            var vaultUrl = Environment.GetEnvironmentVariable(SecureStore.Key_Vault);

            var pubKeyName = Environment.GetEnvironmentVariable(SecureStore.Pub_KeyName);

            if (string.IsNullOrWhiteSpace(vaultUrl))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(vaultUrl));
            }

            if (string.IsNullOrWhiteSpace(pubKeyName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(pubKeyName));
            }

            var credential = CreateCredential();
            var keyClient = new KeyClient(new Uri(vaultUrl), credential);

            var key = keyClient.GetKey(pubKeyName);
            _cryptographyClient = new CryptographyClient(key.Value.Id, credential);
        }

        /// <summary>
        /// Test-only constructor. Production code should use DI with <see cref="IAuthModeService"/> and <see cref="ISecretProvider"/>.
        /// </summary>
        public AzureEncryptionService(CryptographyClient cryptographyClient)
        {
            _cryptographyClient = cryptographyClient ?? throw new ArgumentNullException(nameof(cryptographyClient));
        }

        private TokenCredential CreateCredential()
        {
            if (IsLocal)
            {
                return new AzureCliCredential();
            }

            return new ManagedIdentityCredential();
            //return new DefaultAzureCredential();
        }

        public byte[] Encrypt(byte[] plainData)
        {
            if (plainData == null)
            {
                throw new ArgumentNullException(nameof(plainData));
            }

            var dataKey = GetRandomBytes(AesKeySizeBytes);
            var nonce = GetRandomBytes(NonceSizeBytes);
            var cipherText = new byte[plainData.Length];
            var tag = new byte[TagSizeBytes];

            AesGcmEncrypt(dataKey, nonce, plainData, cipherText, tag);

            var wrappedKey = _cryptographyClient
                .Encrypt(KeyVaultEncryptionAlgorithm.RsaOaep256, dataKey)
                .Ciphertext;

            ZeroMemory(dataKey);

            return PackCipherPayload(wrappedKey, nonce, tag, cipherText);
        }

        public byte[] Decrypt(byte[] cipherData)
        {
            if (cipherData == null)
            {
                throw new ArgumentNullException(nameof(cipherData));
            }

            byte[] wrappedKey;
            byte[] nonce;
            byte[] tag;
            byte[] cipherText;
            UnpackCipherPayload(cipherData, out wrappedKey, out nonce, out tag, out cipherText);

            var dataKey = _cryptographyClient
                .Decrypt(KeyVaultEncryptionAlgorithm.RsaOaep256, wrappedKey)
                .Plaintext;

            try
            {
                var plainData = new byte[cipherText.Length];
                AesGcmDecrypt(dataKey, nonce, cipherText, tag, plainData);
                return plainData;
            }
            finally
            {
                ZeroMemory(dataKey);
            }
        }

        public Task<byte[]> EncryptAsync(byte[] plainData, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (plainData == null)
            {
                throw new ArgumentNullException(nameof(plainData));
            }

            return EncryptInternalAsync(plainData, cancellationToken);
        }

        public Task<byte[]> DecryptAsync(byte[] cipherData, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cipherData == null)
            {
                throw new ArgumentNullException(nameof(cipherData));
            }

            return DecryptInternalAsync(cipherData, cancellationToken);
        }

        private async Task<byte[]> EncryptInternalAsync(byte[] plainData, CancellationToken cancellationToken)
        {
            var dataKey = GetRandomBytes(AesKeySizeBytes);
            var nonce = GetRandomBytes(NonceSizeBytes);
            var cipherText = new byte[plainData.Length];
            var tag = new byte[TagSizeBytes];

            AesGcmEncrypt(dataKey, nonce, plainData, cipherText, tag);

            var wrapResult = await _cryptographyClient
                .EncryptAsync(KeyVaultEncryptionAlgorithm.RsaOaep256, dataKey, cancellationToken)
                .ConfigureAwait(false);

            ZeroMemory(dataKey);

            return PackCipherPayload(wrapResult.Ciphertext, nonce, tag, cipherText);
        }

        private async Task<byte[]> DecryptInternalAsync(byte[] cipherData, CancellationToken cancellationToken)
        {
            byte[] wrappedKey;
            byte[] nonce;
            byte[] tag;
            byte[] cipherText;
            UnpackCipherPayload(cipherData, out wrappedKey, out nonce, out tag, out cipherText);

            var unwrapResult = await _cryptographyClient
                .DecryptAsync(KeyVaultEncryptionAlgorithm.RsaOaep256, wrappedKey, cancellationToken)
                .ConfigureAwait(false);

            var dataKey = unwrapResult.Plaintext;

            try
            {
                var plainData = new byte[cipherText.Length];
                AesGcmDecrypt(dataKey, nonce, cipherText, tag, plainData);
                return plainData;
            }
            finally
            {
                ZeroMemory(dataKey);
            }
        }

        private static void AesGcmEncrypt(
            byte[] key,
            byte[] nonce,
            byte[] plainText,
            byte[] cipherText,
            byte[] tag)
        {
            var gcm = new GcmBlockCipher(new AesEngine());
            gcm.Init(true, new AeadParameters(new KeyParameter(key), TagSizeBytes * 8, nonce, null));

            var output = new byte[gcm.GetOutputSize(plainText.Length)];
            var length = gcm.ProcessBytes(plainText, 0, plainText.Length, output, 0);
            length += gcm.DoFinal(output, length);

            if (length != cipherText.Length + tag.Length)
            {
                throw new CryptographicException("AES-GCM encryption produced unexpected output length.");
            }

            Buffer.BlockCopy(output, 0, cipherText, 0, cipherText.Length);
            Buffer.BlockCopy(output, cipherText.Length, tag, 0, tag.Length);
        }

        private static void AesGcmDecrypt(
            byte[] key,
            byte[] nonce,
            byte[] cipherText,
            byte[] tag,
            byte[] plainText)
        {
            var gcm = new GcmBlockCipher(new AesEngine());
            gcm.Init(false, new AeadParameters(new KeyParameter(key), TagSizeBytes * 8, nonce, null));

            var input = new byte[cipherText.Length + tag.Length];
            Buffer.BlockCopy(cipherText, 0, input, 0, cipherText.Length);
            Buffer.BlockCopy(tag, 0, input, cipherText.Length, tag.Length);

            var output = new byte[gcm.GetOutputSize(input.Length)];
            var length = gcm.ProcessBytes(input, 0, input.Length, output, 0);
            length += gcm.DoFinal(output, length);

            if (length != plainText.Length)
            {
                throw new CryptographicException("AES-GCM decryption produced unexpected output length.");
            }

            Buffer.BlockCopy(output, 0, plainText, 0, plainText.Length);
        }

        private static byte[] GetRandomBytes(int count)
        {
            var bytes = new byte[count];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return bytes;
        }

        private static void ZeroMemory(byte[] buffer)
        {
            if (buffer != null)
            {
                Array.Clear(buffer, 0, buffer.Length);
            }
        }

        private static byte[] PackCipherPayload(byte[] wrappedKey, byte[] nonce, byte[] tag, byte[] cipherText)
        {
            var payload = new byte[
                1 +
                WrappedKeyLengthFieldSize +
                wrappedKey.Length +
                NonceSizeBytes +
                TagSizeBytes +
                cipherText.Length];

            var offset = 0;
            payload[offset++] = FormatVersion;
            WriteInt32BigEndian(payload, ref offset, wrappedKey.Length);
            Buffer.BlockCopy(wrappedKey, 0, payload, offset, wrappedKey.Length);
            offset += wrappedKey.Length;
            Buffer.BlockCopy(nonce, 0, payload, offset, NonceSizeBytes);
            offset += NonceSizeBytes;
            Buffer.BlockCopy(tag, 0, payload, offset, TagSizeBytes);
            offset += TagSizeBytes;
            Buffer.BlockCopy(cipherText, 0, payload, offset, cipherText.Length);

            return payload;
        }

        private static void UnpackCipherPayload(
            byte[] cipherData,
            out byte[] wrappedKey,
            out byte[] nonce,
            out byte[] tag,
            out byte[] cipherText)
        {
            if (cipherData.Length < 1 + WrappedKeyLengthFieldSize + NonceSizeBytes + TagSizeBytes + 1)
            {
                throw new CryptographicException("Ciphertext is too short or malformed.");
            }

            var offset = 0;
            var version = cipherData[offset++];
            if (version != FormatVersion)
            {
                throw new CryptographicException("Unsupported ciphertext format version: " + version + ".");
            }

            var wrappedKeyLength = ReadInt32BigEndian(cipherData, ref offset);
            if (wrappedKeyLength <= 0 || offset + wrappedKeyLength > cipherData.Length)
            {
                throw new CryptographicException("Ciphertext contains an invalid wrapped key length.");
            }

            wrappedKey = new byte[wrappedKeyLength];
            Buffer.BlockCopy(cipherData, offset, wrappedKey, 0, wrappedKeyLength);
            offset += wrappedKeyLength;

            if (offset + NonceSizeBytes + TagSizeBytes >= cipherData.Length)
            {
                throw new CryptographicException("Ciphertext is missing nonce, tag, or payload.");
            }

            nonce = new byte[NonceSizeBytes];
            Buffer.BlockCopy(cipherData, offset, nonce, 0, NonceSizeBytes);
            offset += NonceSizeBytes;

            tag = new byte[TagSizeBytes];
            Buffer.BlockCopy(cipherData, offset, tag, 0, TagSizeBytes);
            offset += TagSizeBytes;

            var cipherTextLength = cipherData.Length - offset;
            cipherText = new byte[cipherTextLength];
            Buffer.BlockCopy(cipherData, offset, cipherText, 0, cipherTextLength);
        }

        private static void WriteInt32BigEndian(byte[] buffer, ref int offset, int value)
        {
            buffer[offset++] = (byte)(value >> 24);
            buffer[offset++] = (byte)(value >> 16);
            buffer[offset++] = (byte)(value >> 8);
            buffer[offset++] = (byte)value;
        }

        private static int ReadInt32BigEndian(byte[] buffer, ref int offset)
        {
            var value =
                (buffer[offset++] << 24) |
                (buffer[offset++] << 16) |
                (buffer[offset++] << 8) |
                buffer[offset++];

            return value;
        }
    }
}
