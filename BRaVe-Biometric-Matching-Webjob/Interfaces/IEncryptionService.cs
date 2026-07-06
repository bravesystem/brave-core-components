using System.Threading;
using System.Threading.Tasks;

namespace BRaVe_Biometric_Matching_Webjob.Interfaces
{

    public interface IEncryptionService
    {
        /// <returns>The decrypted plaintext.</returns>
        byte[] Decrypt(byte[] cipherData);

        /// <summary>
        /// Asynchronously decrypts the specified ciphertext produced by <see cref="EncryptAsync"/>.
        /// </summary>
        /// <param name="cipherData">The encrypted data.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The decrypted plaintext.</returns>
        Task<byte[]> DecryptAsync(byte[] cipherData, CancellationToken cancellationToken = default);
    }

}