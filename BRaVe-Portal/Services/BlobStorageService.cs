using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;

namespace BRaVe_Portal.Services
{
    public sealed class BlobStorageService : IBlobStorageService
    {


        private readonly BlobServiceClient _blobServiceClient;

        // Prefer injecting BlobServiceClient via DI (see registration below)
        public BlobStorageService()
        {

            var connectionString = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Storage.StorageConnection);
         


            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Environment variable 'AZURE_STORAGE_CONNECTION_STRING' is not set or empty.");
            }

            _blobServiceClient = new BlobServiceClient(connectionString);

        }

        /// <summary>
        /// Writes a stream to a blob and sets the content type.
        /// Returns the absolute Uri of the created/overwritten blob.
        /// </summary>
        public async Task<string> WriteAsync(
            string containerName,
            string blobPath,
            Stream content,
            string contentType,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException("Container name is required", nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobPath)) throw new ArgumentException("Blob path is required", nameof(blobPath));
            if (content is null) throw new ArgumentNullException(nameof(content));

            var container = _blobServiceClient.GetBlobContainerClient(containerName);

            // Create container if it does not exist. You can set PublicAccessType if needed.
            await container.CreateIfNotExistsAsync(cancellationToken: ct).ConfigureAwait(false);

            var blob = container.GetBlobClient(blobPath);

            // Reset stream position if possible
            if (content.CanSeek)
                content.Position = 0;

            var headers = new BlobHttpHeaders
            {
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
            };

            // Upload will overwrite by default if called with UploadAsync and overwrite: true (SDK >= 12.14.0),
            // but to keep broad compatibility we use the older pattern: call UploadAsync with conditions: null (throws if exists),
            // or call UploadAsync and then SetHttpHeadersAsync. The clearest is to use UploadAsync with headers.
            await blob.UploadAsync(
                content,
                new BlobUploadOptions
                {
                    HttpHeaders = headers
                },
                ct
            ).ConfigureAwait(false);

            return blob.Uri.ToString();
        }

        /// <summary>
        /// Reads a blob and returns BlobDownloadResult (content + headers + metadata).
        /// Throws RequestFailedException(404) if not found.
        /// </summary>
        public async Task<BlobDownloadResult> ReadAsync(
            string containerName,
            string blobPath,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentException("Container name is required", nameof(containerName));
            if (string.IsNullOrWhiteSpace(blobPath)) throw new ArgumentException("Blob path is required", nameof(blobPath));

            var container = _blobServiceClient.GetBlobContainerClient(containerName);


            // Normalize path (handles full URLs)
            var normalizedBlobPath = NormalizeBlobPath(containerName, blobPath);

            var blob = container.GetBlobClient(normalizedBlobPath);


            // Defensive existence check to produce clearer error than generic 404
            var exists = await blob.ExistsAsync(ct).ConfigureAwait(false);
            if (!exists.Value)
            {
                // Optional: list siblings under the same prefix to help diagnose typos
                // await foreach (var item in container.GetBlobsAsync(prefix: GetParentPrefix(blobPath), cancellationToken: ct))
                //     _logger?.LogInformation("Nearby blob: {Name}", item.Name);

                throw new FileNotFoundException($"Blob not found. Container='{containerName}', BlobPath='{blobPath}', Uri='{blob.Uri}'");
            }


            // This provides a BlobDownloadResult whose Content is BinaryData.
            Response<BlobDownloadResult> response = await blob.DownloadContentAsync(ct).ConfigureAwait(false);

            return response.Value;
        }



        private static string NormalizeBlobPath(string containerName, string blobPath)
        {
            if (string.IsNullOrWhiteSpace(blobPath))
                throw new ArgumentException("Blob path is required.", nameof(blobPath));

            // 1) Trim & URL-decode
            var normalized = blobPath.Trim();
            try
            {
                normalized = Uri.UnescapeDataString(normalized);
            }
            catch { /* ignore */ }

            // 2) If it's a full URL, extract the blob name after the container segment
            if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            {
                // Path is like: /<container>/<blobName...>
                var absolutePath = uri.AbsolutePath; // leading slash included
                if (!string.IsNullOrWhiteSpace(containerName))
                {
                    var prefix = "/" + containerName + "/";
                    if (absolutePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        normalized = absolutePath.Substring(prefix.Length); // just the blob name
                    }
                    else
                    {
                        // Fallback: drop leading slash
                        normalized = absolutePath.TrimStart('/');
                    }
                }
                else
                {
                    // No container available, drop leading slash and use the remainder
                    normalized = absolutePath.TrimStart('/');
                }
            }

            // 3) Standardize separators
            normalized = normalized.Replace('\\', '/');

            // 4) Remove accidental leading slash
            if (normalized.StartsWith("/", StringComparison.Ordinal))
                normalized = normalized.Substring(1);

            return normalized;
        }





    }
}
