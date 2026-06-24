using BRaVe_Mobile_Backend.Interfaces;
using BRaVe_Mobile_Backend.Models;
using System.Security.Claims;

namespace BRaVe_Mobile_Backend.Helpers
{
    public class RequestContextValidator : IRequestContextValidator
    {
        private readonly ILogger<RequestContextValidator> _logger;
        private readonly IDeviceService _deviceService;

        private DeviceProfile _deviceProfile;

        public RequestContextValidator( ILogger<RequestContextValidator> logger, IDeviceService deviceService)
        {
            _logger = logger;
            _deviceService = deviceService;
        }

        public DeviceProfile DeviceProfile() 
        { 
            return _deviceProfile;  
        }

        public async Task<ValidationResult> ValidateAsync(ClaimsPrincipal user, string? requestIp)
        {
            // Auth
            if (!(user?.Identity?.IsAuthenticated ?? false))
                return ValidationResult.Unauthenticated("Unauthenticated");

            var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var tokenTenant = user.FindFirst("tenantId")?.Value;
            var tokenDevice = user.FindFirst("deviceId")?.Value;

            // Device ownership

            if (string.IsNullOrEmpty(tokenDevice) )
            {
                return ValidationResult.NotFound("DeviceNotFound");
            }

            _deviceProfile = await _deviceService.GetByIdAsync(tokenDevice);

            if( _deviceProfile == null )
                return ValidationResult.NotFound("DeviceNotFound");

            // Tenant scope
            if (!string.IsNullOrEmpty(tokenTenant) && int.TryParse(tokenTenant, out var tenantFromToken))
            {
                if (tenantFromToken != _deviceProfile.TenantId)
                    return ValidationResult.Forbidden("Tenant mismatch");
            }


            return ValidationResult.Success();
        }

    }
}
