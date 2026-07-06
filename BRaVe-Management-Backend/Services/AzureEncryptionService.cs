using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using BRaVe_Management_Backend.Interfaces;
using System.Security.Cryptography;
using static BRaVe_Management_Backend.Helpers.KeyVaultSecretNames;
using KeyVaultEncryptionAlgorithm = Azure.Security.KeyVault.Keys.Cryptography.EncryptionAlgorithm;


namespace BRaVe_Management_Backend.Services
{
    public sealed class AzureEncryptionService : IEncryptionService
    {
        private const byte FormatVersion = 1;
        private const int AesKeySizeBytes = 32;
        private const int NonceSizeBytes = 12;
        private const int TagSizeBytes = 16;
        private const int WrappedKeyLengthFieldSize = 4;

        private readonly CryptographyClient _cryptographyClient;
        private readonly ISecretProvider? _secretProvider;
        private readonly IAuthModeService? _authModeService;

        public AzureEncryptionService(IAuthModeService authModeService, ISecretProvider secretProvider)
        {
            ArgumentNullException.ThrowIfNull(authModeService);
            ArgumentNullException.ThrowIfNull(secretProvider);

            _authModeService = authModeService;
            _secretProvider = secretProvider;

            // Same sync pattern as SqlUatFormAccessService / Redis DI factory in Program.cs
            var vaultUrl = _secretProvider.GetSecretAsync(SecureStore.Key_Vault).GetAwaiter().GetResult();
            var pubKeyName = _secretProvider.GetSecretAsync(SecureStore.Pub_KeyName).GetAwaiter().GetResult();

            ArgumentException.ThrowIfNullOrWhiteSpace(vaultUrl);
            ArgumentException.ThrowIfNullOrWhiteSpace(pubKeyName);

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
            if (_authModeService!.IsMockMode())
            {
                return new AzureCliCredential();
            }

            return new DefaultAzureCredential();
        }

        public byte[] Encrypt(byte[] plainData)
        {
            ArgumentNullException.ThrowIfNull(plainData);

            var dataKey = RandomNumberGenerator.GetBytes(AesKeySizeBytes);
            var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
            var cipherText = new byte[plainData.Length];
            var tag = new byte[TagSizeBytes];

            using (var aesGcm = new AesGcm(dataKey, TagSizeBytes))
            {
                aesGcm.Encrypt(nonce, plainData, cipherText, tag);
            }

            var wrappedKey = _cryptographyClient.Encrypt(


                KeyVaultEncryptionAlgorithm.RsaOaep256, dataKey).Ciphertext;
            CryptographicOperations.ZeroMemory(dataKey);

            return PackCipherPayload(wrappedKey, nonce, tag, cipherText);
        }

        public byte[] Decrypt(byte[] cipherData)
        {
            ArgumentNullException.ThrowIfNull(cipherData);

            UnpackCipherPayload(cipherData, out var wrappedKey, out var nonce, out var tag, out var cipherText);

            var dataKey = _cryptographyClient.Decrypt(KeyVaultEncryptionAlgorithm.RsaOaep256, wrappedKey).Plaintext;

            try
            {
                var plainData = new byte[cipherText.Length];
                using var aesGcm = new AesGcm(dataKey, TagSizeBytes);
                aesGcm.Decrypt(nonce, cipherText, tag, plainData);
                return plainData;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(dataKey);
            }
        }

        public Task<byte[]> EncryptAsync(byte[] plainData, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(plainData);
            return EncryptInternalAsync(plainData, cancellationToken);
        }

        public Task<byte[]> DecryptAsync(byte[] cipherData, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(cipherData);
            return DecryptInternalAsync(cipherData, cancellationToken);
        }

        private async Task<byte[]> EncryptInternalAsync(byte[] plainData, CancellationToken cancellationToken)
        {
            var dataKey = RandomNumberGenerator.GetBytes(AesKeySizeBytes);
            var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
            var cipherText = new byte[plainData.Length];
            var tag = new byte[TagSizeBytes];

            using (var aesGcm = new AesGcm(dataKey, TagSizeBytes))
            {
                aesGcm.Encrypt(nonce, plainData, cipherText, tag);
            }

            var wrapResult = await _cryptographyClient
                .EncryptAsync(KeyVaultEncryptionAlgorithm.RsaOaep256, dataKey, cancellationToken)
                .ConfigureAwait(false);

            CryptographicOperations.ZeroMemory(dataKey);

            return PackCipherPayload(wrapResult.Ciphertext, nonce, tag, cipherText);
        }

        private async Task<byte[]> DecryptInternalAsync(byte[] cipherData, CancellationToken cancellationToken)
        {
            UnpackCipherPayload(cipherData, out var wrappedKey, out var nonce, out var tag, out var cipherText);

            var unwrapResult = await _cryptographyClient
                .DecryptAsync(KeyVaultEncryptionAlgorithm.RsaOaep256, wrappedKey, cancellationToken)
                .ConfigureAwait(false);

            var dataKey = unwrapResult.Plaintext;

            try
            {
                var plainData = new byte[cipherText.Length];
                using var aesGcm = new AesGcm(dataKey, TagSizeBytes);
                aesGcm.Decrypt(nonce, cipherText, tag, plainData);
                return plainData;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(dataKey);
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
                throw new CryptographicException($"Unsupported ciphertext format version: {version}.");
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
