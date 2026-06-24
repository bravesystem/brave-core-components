using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using static System.Net.WebRequestMethods;

namespace BRaVe_Portal.Services
{
    public sealed class AccessControlClient : IAccessControlClient
    {
        private readonly IRestApiService _api;
        public AccessControlClient(IRestApiService api) => _api = api;

        public async Task<TenantAndRolesDto> GetRolesAsync(string userId, string language, CancellationToken ct = default)
        {
            var res = await _api.GetAsync<TenantAndRolesDto>($"v1/AccessControl/{Uri.EscapeDataString(userId)}/{language}", ct);

            return res;
        }

        public async Task<TenantAndRolesDto> GetFormAuthRolesAsync(string userId, string language, CancellationToken ct = default)
        {
            var res = await _api.GetAsync<TenantAndRolesDto>($"v1/FormAccess/roles/{Uri.EscapeDataString(userId)}/{language}", ct);

            return res;
        }

        public async Task FormRegisterUser(UatFormRegisterDto dto, CancellationToken ct = default)
        {
            var response = await _api.PostJsonAsync<UatFormRegisterDto, HttpResponseMessage>("v1/FormAccess/register", dto, ct);

            response.EnsureSuccessStatusCode();
        }

        public async Task UpdateLogoutTimeAsync(CancellationToken ct = default)
        {
            var response = await _api.PostJsonAsync<object, HttpResponseMessage>(
                $"v1/AccessControl/update-logout-time",
                null,
                ct
            );

            response.EnsureSuccessStatusCode();
        }


    }
}
