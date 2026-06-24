using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
    public interface IDistributionservice
    {
        /// <summary>
        /// Retrieves all Distributions for a given program.
        /// </summary>
        Task<IEnumerable<Distributions>> GetAllDistributions(int programId);

        /// <summary>
        /// Retrieves a Distributions by its unique ID.
        /// </summary>
        Task<Distributions?> GetDistributionsById(int id);

        /// <summary>
        /// Creates a new Distributions.
        /// </summary>
        Task CreateDistributions(Distributions Distributions);

        /// <summary>
        /// Updates an existing Distributions.
        /// </summary>
        Task UpdateDistributions(Distributions Distributions);

        /// <summary>
        /// Deletes a Distributions by ID.
        /// </summary>
        Task DeleteDistributions(int id);
    }
}