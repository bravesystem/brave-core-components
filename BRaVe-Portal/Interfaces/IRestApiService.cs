using System.Net;
using System.Net.Http;

namespace BRaVe_Portal.Interfaces
{
    public interface IRestApiService
    {
        Task<T> GetAsync<T>(string path, CancellationToken ct = default);
        Task<TResponse> PostJsonAsync<TResponse>(string path, CancellationToken ct = default);
        Task<TResponse> PostJsonAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default);
        Task<TResponse> PutJsonAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default);
        Task DeleteAsync(string path, CancellationToken ct = default);

        // Optional: raw access when you don’t know the shape
        Task<(HttpStatusCode Status, string Body)> GetRawAsync(string path, CancellationToken ct = default);

        // For file uploads
        Task<HttpResponseMessage> PostFileAsync(string path, IFormFile file, CancellationToken ct = default);

        Task<byte[]> GetFileAsync(string path, CancellationToken ct = default);



    }
}
