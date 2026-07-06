namespace BRaVe_Management_Backend.Interfaces
{
    public interface IEncryptionService
    {  /// <summary>
       /// Encrypts the specified plaintext.
       /// </summary>
       /// <param name="plainData">The data to encrypt.</param>
       /// <returns>The encrypted ciphertext.</returns>
        byte[] Encrypt(byte[] plainData);

        /// <summary>
        /// Decrypts the specified ciphertext produced by <see cref="Encrypt"/>.
        /// </summary>
        /// <param name="cipherData">The encrypted data.</param>
        /// <returns>The decrypted plaintext.</returns>
        byte[] Decrypt(byte[] cipherData);

        /// <summary>
        /// Asynchronously encrypts the specified plaintext.
        /// </summary>
        /// <param name="plainData">The data to encrypt.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The encrypted ciphertext.</returns>
        Task<byte[]> EncryptAsync(byte[] plainData, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously decrypts the specified ciphertext produced by <see cref="EncryptAsync"/>.
        /// </summary>
        /// <param name="cipherData">The encrypted data.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The decrypted plaintext.</returns>
        Task<byte[]> DecryptAsync(byte[] cipherData, CancellationToken cancellationToken = default);

    }
}
