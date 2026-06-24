using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.Interfaces
{
        public interface IDatasetService
        {
            /// <summary>
            /// Get all datasets for the current tenant
            /// </summary>
            Task<List<DatasetsDto>> GetAllAsync(int tenantId);

            /// <summary>
            /// Get a dataset by Id
            /// </summary>
            Task<DatasetsDto?> GetByIdAsync(int id, int tenantId);

            /// <summary>
            /// Create a new dataset
            /// </summary>
            Task<int> CreateAsync(DatasetsDto dataset, int tenantId, string userId);

            /// <summary>
            /// Update an existing dataset
            /// </summary>
            Task<bool> UpdateAsync(DatasetsDto dataset, int tenantId, string userId);

            /// <summary>
            /// Soft delete / deactivate dataset
            /// </summary>
            Task<bool> DeactivateAsync(int id, int tenantId, string userId);
        }
    }