using Azure.Storage.Blobs.Models;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BRaVe_Portal.Interfaces
{
    public interface IBlobStorageService
    {
        /// <summary>
        /// Uploads a file to a container and returns the blob URL.
        /// </summary>
        Task<string> WriteAsync(
            string containerName,
            string blobPath,
            Stream content,
            string contentType,
            CancellationToken ct = default
        );

        /// <summary>
        /// Reads a blob and returns it for download.
        /// </summary>
        Task<BlobDownloadResult> ReadAsync(
            string containerName,
            string blobPath,
            CancellationToken ct = default
        );
    }
}

