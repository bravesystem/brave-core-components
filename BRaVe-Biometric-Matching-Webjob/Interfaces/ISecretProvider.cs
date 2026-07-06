using System.Threading;
using System.Threading.Tasks;

namespace BRaVe_Biometric_Matching_Webjob.Interfaces
{
    /// <summary>
    /// Retrieves secret values from Azure Key Vault by secret name.
    /// </summary>
    public interface ISecretProvider
    {
        /// <summary>
        /// Gets the value of the specified secret.
        /// </summary>
        /// <param name="secretName">The name of the secret in Key Vault.</param>
        /// <returns>The secret value.</returns>
        string GetSecret(string secretName);

        /// <summary>
        /// Asynchronously gets the value of the specified secret.
        /// </summary>
        /// <param name="secretName">The name of the secret in Key Vault.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The secret value.</returns>
        Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
    }
}
