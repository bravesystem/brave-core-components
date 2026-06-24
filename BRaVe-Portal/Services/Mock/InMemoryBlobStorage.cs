using Azure.Storage.Blobs.Models;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace Services.Mock
{
    /// <summary>
    /// In-memory mock implementation of IBlobStorageService.
    /// Intended for development and testing before Azure Blob Storage is provisioned.
    /// </summary>
    public sealed class InMemoryBlobStorage : IBlobStorageService
    {
        private readonly ConcurrentDictionary<string, StoredBlob> _store = new();

        public Task<string> WriteAsync(
            string containerName,
            string blobPath,
            Stream content,
            string contentType,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (content == null)
                throw new ArgumentNullException(nameof(content));

            using var ms = new MemoryStream();
            content.CopyTo(ms);

            var key = BuildKey(containerName, blobPath);

            _store[key] = new StoredBlob
            {
                Bytes = ms.ToArray(),
                ContentType = contentType,
                FileName = Path.GetFileName(blobPath)
            };

            // Fake but realistic blob URL
            var fakeUrl = $"https://mockstorage.local/{containerName}/{blobPath}";
            return Task.FromResult(fakeUrl);
        }

        public Task<BlobDownloadResult> ReadAsync(
            string containerName,
            string blobPath,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var key = BuildKey(containerName, blobPath);

            var content = "Hello, this is a test file.";
            //var bytes = System.Text.Encoding.UTF8.GetBytes(content);

            /* return Task.FromResult(new BlobDownloadResult
             {
                 Content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)),
                 ContentType = "text/plain",
                 FileName = "sample.txt"
             });*/

            return null;
        }

        /*        public BlobType BlobType { get; internal set; }

        /// <summary>
        /// The number of bytes present in the response body.
        /// </summary>
        public long ContentLength { get; internal set; }

        /// <summary>
        /// The media type of the body of the response. For Download Blob this is 'application/octet-stream'
        /// </summary>
        public string ContentType { get; internal set; }*/

        private static string BuildKey(string container, string path)
            => $"{container.Trim().ToLowerInvariant()}/{path.Trim()}";

        private sealed class StoredBlob
        {
            public required byte[] Bytes { get; init; }
            public required string ContentType { get; init; }
            public required string FileName { get; init; }
        }
    }

   /* public sealed class BlobDownloadResult
    {
        public required Stream Content { get; init; }
        public required string ContentType { get; init; }
        public required string FileName { get; init; }
    }*/
    }
