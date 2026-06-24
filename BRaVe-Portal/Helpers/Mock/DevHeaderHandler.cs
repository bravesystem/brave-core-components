using BRaVe_Portal.Models.DTOs;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace BRaVe_Portal.Helpers.Mock
{
    public class DevHeaderHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _ctx;
        private readonly ILogger<DevHeaderHandler> _logger;

        public DevHeaderHandler(IHttpContextAccessor ctx, ILogger<DevHeaderHandler> logger)
        {
            _ctx = ctx;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var email = _ctx.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ?? "dev@local";
            var name = _ctx.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Mr. T";
            var tenant = _ctx.HttpContext?.User?.FindFirstValue("Tenant") ?? "0";

            var roles = _ctx.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(c => c.Value);
            var rolesCsv = roles != null ? string.Join(",", roles) : string.Empty;

            request.Headers.Authorization = new AuthenticationHeaderValue("Dev", $"{email}|{name}|{rolesCsv}|{tenant}");

            // Optional logging for development/debugging
            _logger.LogDebug(
                "DevHeaderHandler set Authorization header: Email={Email}, Name={Name}, Roles={Roles}, Tenant={Tenant}",
                email, name, rolesCsv, tenant);

            return await base.SendAsync(request, ct);
        }
    }
}
