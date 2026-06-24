using BRaVe_CardPrinting_DesktopApp.Helpers;
using BRaVe_CardPrinting_DesktopApp.Models.Request;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace BRaVe_CardPrinting_DesktopApp.Api
{
    public class CardPrintingApiClient
    {
        private readonly HttpClient _httpClient;

        public CardPrintingApiClient(string baseUrl)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(15)
            };
        }


        public void SetAccessToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        // ==========================================
        // CLAIM DEVICE (LOGIN)
        // ==========================================
        public async Task<PrintDeviceClaimResult?> ClaimAsync(PrintClaimRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "v1/card-printing/claim",
                request
            );

            

            if (!response.IsSuccessStatusCode)
            {
                var problem = await response.Content.ReadFromJsonAsync<ApiProblem>();

                string message = problem?.Title ?? "Request failed";

                throw new Exception(message);
            }

            var raw = await response.Content.ReadAsStringAsync();

            var result = System.Text.Json.JsonSerializer.Deserialize<PrintDeviceClaimResult>(
                raw,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            return result;


        }


        public async Task<string> RefreshAsync(string refreshToken)
        {

        
            var response = await _httpClient.PostAsJsonAsync(
                "v1/card-printing/refresh",
                new RefreshPrintRequest
                {
                    RefreshToken = refreshToken
                }
            );

            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception("Session expired");

            var result = System.Text.Json.JsonSerializer.Deserialize<RefreshResponse>(
                raw,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return result?.AccessToken ?? "";
        }


        public async Task<List<CardPrintQueueItem>> LoadQueueAsync(CardPrintQueueRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "v1/card-printing/load",
                request
            );

            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception("Failed to load records");

            var result = System.Text.Json.JsonSerializer.Deserialize<List<CardPrintQueueItem>>(
                raw,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return result ?? new List<CardPrintQueueItem>();
        }


        public async Task UnlockQueueAsync(List<string> householdIds,string deviceId,string windowsUser)
        {
            var request = new
            {
                HouseholdIds = householdIds,
                DeviceId = deviceId,
                WindowsUser = windowsUser
            };

            var response = await _httpClient.PostAsJsonAsync(
                "v1/card-printing/unlock",
                request
            );

            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception(
                    "Unlock failed: " + raw);
        }

        public async Task SyncPrintedAsync(List<string> householdIds, string deviceId, string windowsUser)
        {
            var request = new
            {
                HouseholdIds = householdIds,
                DeviceId = deviceId,
                WindowsUser = windowsUser
            };

            var response = await _httpClient.PostAsJsonAsync(
                "v1/card-printing/sync",
                request
            );

            var raw = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception("Sync failed: " + raw);
        }

    }
}