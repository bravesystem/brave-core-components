using Azure;
using BRaVe_Portal.Exceptions;
using BRaVe_Portal.Interfaces;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office2013.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Reflection.Metadata;
using System.Text.Json;

namespace BRaVe_Portal.Services
{
    public class ManagementApiService : IRestApiService
    {
        private readonly ILogger<ManagementApiService> _logger;
        private readonly HttpClient _http;

        private readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ManagementApiService(ILogger<ManagementApiService> logger, HttpClient http)
        {
            _logger = logger;
            _http = http;
        }

        public async Task<T> GetAsync<T>(string path, CancellationToken ct = default)
        {
            _logger.LogInformation("GET request to {Path}", path);
            using var res = await _http.GetAsync(path, ct);

            //If auth required, do not continue. Let middleware turn 401 → /Login.
            //if (res.StatusCode == HttpStatusCode.Unauthorized ||
            //    res.StatusCode == HttpStatusCode.Forbidden)
            //{
            //    _logger.LogInformation("GET {Path} returned {Status}. Short-circuiting for auth redirect.", path, res.StatusCode);
            //    return default!;
            //}


            await EnsureSuccessOrThrow(res, ct);
          

            var payload = await res.Content.ReadFromJsonAsync<T>(_json, ct);
            _logger.LogInformation("GET request to {Path} completed with status {Status}", path, res.StatusCode);
            return payload is not null ? payload : throw new InvalidOperationException("Empty response body.");
        }

        public async Task<TResponse> PostJsonAsync<TResponse>(string path, CancellationToken ct = default)
        {
            _logger.LogInformation("POST request to {Path} (no body)", path);
            using var res = await _http.PostAsync(path, null, ct);
            await EnsureSuccessOrThrow(res, ct);

            if (res.StatusCode == HttpStatusCode.NoContent || res.Content?.Headers.ContentLength == 0)
            {
                _logger.LogInformation("POST request to {Path} returned no content", path);
                return default!;
            }

            var payload = await res.Content.ReadFromJsonAsync<TResponse>(_json, ct);
            _logger.LogInformation("POST request to {Path} completed with status {Status}", path, res.StatusCode);
            return payload is not null ? payload : throw new InvalidOperationException("Empty response body.");
        }

        public async Task<TResponse> PostJsonAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default)
        {
            _logger.LogInformation("POST request to {Path} with body {@Body}", path, body);
            using var res = await _http.PostAsJsonAsync(path, body, _json, ct);
            await EnsureSuccessOrThrow(res, ct);

            if (res.StatusCode == HttpStatusCode.NoContent || res.Content?.Headers.ContentLength == 0)
            {
                _logger.LogInformation("POST request to {Path} returned no content", path);
                return default!;
            }

            var payload = await res.Content.ReadFromJsonAsync<TResponse>(_json, ct);
            _logger.LogInformation("POST request to {Path} completed with status {Status}", path, res.StatusCode);
            return payload is not null ? payload : throw new InvalidOperationException("Empty response body.");
        }

        public async Task<TResponse> PutJsonAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default)
        {
            _logger.LogInformation("PUT request to {Path} with body {@Body}", path, body);
            using var res = await _http.PutAsJsonAsync(path, body, _json, ct);
            await EnsureSuccessOrThrow(res, ct);

            if (res.StatusCode == HttpStatusCode.NoContent || res.Content?.Headers.ContentLength == 0)
            {
                _logger.LogInformation("PUT request to {Path} returned no content", path);
                return default!;
            }

            var payload = await res.Content.ReadFromJsonAsync<TResponse>(_json, ct);
            _logger.LogInformation("PUT request to {Path} completed with status {Status}", path, res.StatusCode);
            return payload is not null ? payload : throw new InvalidOperationException("Empty response body.");
        }

        public async Task DeleteAsync(string path, CancellationToken ct = default)
        {
            _logger.LogInformation("DELETE request to {Path}", path);
            using var res = await _http.DeleteAsync(path, ct);
            await EnsureSuccessOrThrow(res, ct);
            _logger.LogInformation("DELETE request to {Path} completed with status {Status}", path, res.StatusCode);
        }

        public async Task<(HttpStatusCode Status, string Body)> GetRawAsync(string path, CancellationToken ct = default)
        {
            _logger.LogInformation("GET RAW request to {Path}", path);
            using var res = await _http.GetAsync(path, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("GET RAW request to {Path} returned status {Status}", path, res.StatusCode);
            return (res.StatusCode, body);
        }

        public async Task<HttpResponseMessage> PostFileAsync(string path, IFormFile file, CancellationToken ct = default)
        {
            _logger.LogInformation("POST file upload request to {Path}", path);

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("No file provided for upload to {Path}", path);
                throw new ArgumentException("Invalid file upload request.");
            }

            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();

            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            content.Add(streamContent, "file", file.FileName);

            var res = await _http.PostAsync(path, content, ct); // 🟢 note: no using, we’re returning it

            _logger.LogInformation("POST file upload to {Path} completed with status {Status}", path, res.StatusCode);

            // Log error but DO NOT throw — let caller handle non-success
            if (!res.IsSuccessStatusCode)
            {
                var raw = await res.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("File upload failed with status {Status}: {Error}", res.StatusCode, raw);
            }

            return res;
        }

        public async Task<byte[]> GetFileAsync(string path, CancellationToken ct = default)
        {
            _logger.LogInformation("GET FILE request to {Path}", path);

            using var res = await _http.GetAsync(path, ct);

            await EnsureSuccessOrThrow(res, ct);

            var bytes = await res.Content.ReadAsByteArrayAsync(ct);

            _logger.LogInformation("GET FILE request to {Path} completed with status {Status}", path, res.StatusCode);

            return bytes;
        }




        private async Task EnsureSuccessOrThrow(HttpResponseMessage res, CancellationToken ct)
        {
            if (res.IsSuccessStatusCode)
                return;


            if (res.StatusCode == HttpStatusCode.Unauthorized || res.StatusCode == HttpStatusCode.Forbidden)
            {
                // Optional: log at Information/Warning (not Error), because this is an expected auth flow
                _logger.LogInformation("Downstream API returned {Status}. Letting middleware/controller handle redirect.", res.StatusCode);
                throw new RedirectToLoginException();
            }



            var raw = await res.Content.ReadAsStringAsync(ct);

            _logger.LogError("API request failed: {Status} {Body}", (int)res.StatusCode, raw);

            //_logger.LogError("API request failed: {Message}", raw);

            //throw new HttpRequestException(msg, null, res.StatusCode);
            throw new HttpRequestException(raw, null, res.StatusCode);
        }

        public Task PutAsync(string endpoint, object? data)
        {
            throw new NotImplementedException();
        }
    }
}
