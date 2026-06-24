using Azure.Core;
using BRaVe_Management_Backend.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Data;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace BRaVe_Management_Backend.Helpers.Mock
{
    public class DevApiAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IRoleService _roles;

        public DevApiAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> o,
             IRoleService roles,
            ILoggerFactory l, UrlEncoder e, ISystemClock c) : base(o, l, e, c) 
        {
            _roles = roles;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var auth = Request.Headers["Authorization"].ToString();
            if (auth.StartsWith("Dev ", StringComparison.OrdinalIgnoreCase) || auth.StartsWith("Bearer Dev ", StringComparison.OrdinalIgnoreCase))

            {
                var token = auth.Replace("Bearer ", "")
                .Replace("Dev ", "").Trim().Split("|");

                var email = token[0];
                var name = token.Length > 1 ? token[1] : "Anonymous";
                var roles = token.Length > 2 ? token[2].Split(","):[];
                int tenant = 0;
                if(token.Length > 3 )
                    int.TryParse(token[3], out tenant);


                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, email),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Name, name),
                    new Claim("Tenant", tenant.ToString()),
                    new Claim("scp", "Access.Read")
                };

                foreach (var r in roles)
                    claims.Add(new(ClaimTypes.Role, r));


                var id = new ClaimsIdentity(claims.ToArray(), "Dev");
                var principal = new ClaimsPrincipal(id);
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Dev")));
            }
                

            return Task.FromResult(AuthenticateResult.NoResult());

        }
    }
}
