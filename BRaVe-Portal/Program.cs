using BRaVe_Portal.Exceptions;
using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Services;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Serilog;
using Services.Mock;
using StackExchange.Redis;
using System.Globalization;
using System.Net;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);

var cfg = builder.Configuration;

var aiConnectionString = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Logging.App_Insight);
var websiteHostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") ?? string.Empty;
var appEnvironment = Environment.GetEnvironmentVariable("Environment") ?? string.Empty;

var isFormOnlyAuth =
    string.Equals(appEnvironment, "PARTNER", StringComparison.OrdinalIgnoreCase)
    || websiteHostname.Contains("-ptr-", StringComparison.OrdinalIgnoreCase);

if (isFormOnlyAuth && !string.Equals(appEnvironment, "PARTNER", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine(
        $"WARNING: Hostname '{websiteHostname}' indicates partner portal but Environment is '{(string.IsNullOrEmpty(appEnvironment) ? "(not set)" : appEnvironment)}'. Forcing form-only auth.");
    appEnvironment = "PARTNER";
}

builder.Services.AddSingleton(appEnvironment);

var loggerConfig = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console();

if (!string.IsNullOrWhiteSpace(aiConnectionString))
{
    loggerConfig = loggerConfig.WriteTo.ApplicationInsights(
        new TelemetryConfiguration { ConnectionString = aiConnectionString },
        TelemetryConverter.Traces);
}
else
{
    Console.WriteLine("WARNING: Application Insights connection string is not configured.");
}

Log.Logger = loggerConfig.CreateLogger();

builder.Host.UseSerilog();


// Toggle with env var: Auth__Mode=Mock or Real
var useMock = builder.Environment.IsDevelopment();

IAuthModeService authModeService = new AuthModeService(useMock);

builder.Services.AddSingleton(authModeService);

Log.Information(
    "Portal startup: Environment={Environment}, Hostname={Hostname}, FormOnlyAuth={FormOnlyAuth}",
    appEnvironment,
    websiteHostname,
    isFormOnlyAuth);

if (isFormOnlyAuth)
{
    Log.Information("PARTNER environment: form-only authentication enabled. Microsoft Identity Web / OIDC is disabled.");

    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Login";
            options.AccessDeniedPath = "/Login";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });
}
else
{
    // Manually create and populate AzureAdOptions from environment variables
    var azureAdOptions = new AzureAdOptions
    {
        Instance = Environment.GetEnvironmentVariable(KeyVaultSecretNames.AzureAd.Fe_Instance)
                            ?? "https://login.microsoftonline.com/",
        TenantId = Environment.GetEnvironmentVariable(KeyVaultSecretNames.AzureAd.Fe_Tenant_Id) ?? "",
        ClientId = Environment.GetEnvironmentVariable(KeyVaultSecretNames.AzureAd.Fe_Client_Id) ?? "",
        ClientSecret = Environment.GetEnvironmentVariable(KeyVaultSecretNames.AzureAd.Fe_Client_Secret) ?? "",
        ApplicationUri = "",
        CallbackPath = Environment.GetEnvironmentVariable(KeyVaultSecretNames.AzureAd.Fe_Callback) ?? "/signin-oidc",
        // Expect comma-separated scopes: e.g., "api://xxx/.default,User.Read"
        Scopes = (Environment.GetEnvironmentVariable(KeyVaultSecretNames.AzureAd.Fe_Audience_Scope) ?? "")
                            .Split([','], StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .ToArray()
    };

    // Register the options instance into DI
    builder.Services.AddSingleton(Options.Create(azureAdOptions));


    Log.Information(
        "Microsoft Identity Web config: Instance={Instance}, TenantId={TenantId}, ClientId={ClientId}, CallbackPath={CallbackPath}, ClientSecretConfigured={HasClientSecret}, ScopeCount={ScopeCount}, AppEnvironment={AppEnvironment}",
        azureAdOptions.Instance,
        azureAdOptions.TenantId,
        MaskClientId(azureAdOptions.ClientId),
        azureAdOptions.CallbackPath,
        !string.IsNullOrEmpty(azureAdOptions.ClientSecret),
        azureAdOptions.Scopes.Length,
        appEnvironment);

    if (IsMultiTenantAuthority(azureAdOptions.TenantId))
    {
        Log.Information(
            "OIDC tenant '{TenantId}' supports Any Entra org + personal Microsoft accounts. Authority: {Authority}",
            azureAdOptions.TenantId,
            BuildOidcAuthority(azureAdOptions.Instance, azureAdOptions.TenantId));
    }
    else if (!string.IsNullOrEmpty(azureAdOptions.TenantId))
    {
        Log.Warning(
            "OIDC tenant '{TenantId}' is a fixed tenant GUID. If the app registration is 'Any Entra ID tenant + Personal Microsoft accounts', Fe_Tenant_Id should usually be 'common'.",
            azureAdOptions.TenantId);
    }

    //builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    builder.Services.AddAuthentication(options =>
    {
        // Use Cookies for authenticating each request so both AAD (post sign-in) and UAT form login cookies are read.
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        // When [Authorize] triggers a challenge, send to OIDC (Azure AD).
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddMicrosoftIdentityWebApp(options =>
    {

        // Map from your AzureAdOptions to MicrosoftIdentityOptions
        options.Instance = azureAdOptions.Instance;
        options.TenantId = azureAdOptions.TenantId;
        options.ClientId = azureAdOptions.ClientId;
        options.ClientSecret = azureAdOptions.ClientSecret;
        options.CallbackPath = azureAdOptions.CallbackPath;

        //new addition

        options.Events.OnRedirectToIdentityProvider = context =>
        {
            Log.Information(
                "OIDC OnRedirectToIdentityProvider: Path={Path}, Authority={Authority}, RedirectUri={RedirectUri}, Scopes={Scopes}, Prompt={Prompt}",
                context.HttpContext.Request.Path,
                context.ProtocolMessage.IssuerAddress,
                context.ProtocolMessage.RedirectUri,
                context.ProtocolMessage.Scope,
                context.ProtocolMessage.Prompt);
            return Task.CompletedTask;
        };

        options.Events.OnAuthorizationCodeReceived = context =>
        {
            Log.Information(
                "OIDC OnAuthorizationCodeReceived: Path={Path}, HasIdToken={HasIdToken}, HasAccessToken={HasAccessToken}",
                context.HttpContext.Request.Path,
                !string.IsNullOrEmpty(context.ProtocolMessage.IdToken),
                !string.IsNullOrEmpty(context.ProtocolMessage.AccessToken));
            return Task.CompletedTask;
        };

        options.Events.OnAuthenticationFailed = context =>
        {
            Log.Error(context.Exception,
                "OIDC OnAuthenticationFailed: Path={Path}, Message={Message}",
                context.HttpContext.Request.Path,
                context.Exception.Message);
            return Task.CompletedTask;
        };

        options.Events.OnRemoteFailure = context =>
        {
            string? errorDescription = null;
            context.Properties?.Items.TryGetValue(".error_description", out errorDescription);

            Log.Error(context.Failure,
                "OIDC OnRemoteFailure: Path={Path}, Error={Error}, ErrorDescription={ErrorDescription}",
                context.HttpContext.Request.Path,
                context.Failure?.Message,
                errorDescription);
            return Task.CompletedTask;
        };

        options.Events.OnAccessDenied = context =>
        {
            Log.Warning(
                "OIDC OnAccessDenied: Path={Path}, Properties={Properties}",
                context.HttpContext.Request.Path,
                string.Join(", ", context.Properties.Items.Select(kv => $"{kv.Key}={kv.Value}")));
            return Task.CompletedTask;
        };

        // Build service provider to resolve JwsSignerService
        var serviceProvider = builder.Services.BuildServiceProvider();
        var _accessCtrlClient = serviceProvider.GetRequiredService<IAccessControlClient>();

        options.Events.OnTokenValidated = async context =>
        {
            context.HttpContext.User = context.Principal!;

            var userId = context.Principal!.Identifier();
            var email = context.Principal!.GetUserEmailLike();
            var name = context.Principal!.DisplayName();

            //new addition
            var issuer = context.Principal!.FindFirst("iss")?.Value;
            var tid = context.Principal!.FindFirst("tid")?.Value;
            var utid = context.Principal!.FindFirst("utid")?.Value;
            var idp = context.Principal!.FindFirst("idp")?.Value;
            var accountType = context.Principal!.FindFirst("acct")?.Value;


            Log.Information(
               "OIDC OnTokenValidated: UserId={UserId}, Email={Email}, Name={Name}, Issuer={Issuer}, tid={Tid}, utid={Utid}, idp={Idp}, acct={Acct}",
               userId, email, name, issuer, tid, utid, idp, accountType);


            // Call your backend to get roles
            TenantAndRolesDto tenantAndRoles;
            var roleList = new List<string>
            {
            EnumUserRoles.NoAuth.ToString()
            };

            string TenantId = "0";

            UserRoleInfo userRoleInfo = null;

            try
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                // Authorized call to your API to fetch roles for this user
                tenantAndRoles = await _accessCtrlClient.GetRolesAsync(userId!, languageCode, context.HttpContext.RequestAborted);

                roleList = tenantAndRoles.Roles
                .Select(r => EnumHelper.ToString<EnumUserRoles>(r, EnumUserRoles.NoAuth.ToString()))
                .ToList();

                TenantId = tenantAndRoles.TenantToString();

                userRoleInfo = tenantAndRoles.UserProfile;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                //Log.Logger.Information(ex.Message);
                //Log.Logger.Information($"No role assigned for user {email} with OID {userId}.");
                Log.Warning(ex,
                    "OIDC OnTokenValidated: no roles found for UserId={UserId}, Email={Email}",
                    userId, email);

            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
            {
                //Log.Logger.Error(ex.Message);
                //Log.Logger.Error("Invalid request: Roles cannot be assigned because no tenant is associated(TenantId = 0) or due to a backend connectivity issue. {ex}", ex);
                Log.Error(ex,
                    "OIDC OnTokenValidated: role lookup failed (400) for UserId={UserId}, Email={Email}",
                    userId, email);
                throw ex;
            }

            var identity = (ClaimsIdentity)context.Principal.Identity!;

            string rolesText = "No Roles";
            string missionName = "No Mission Assigned";

            if (userRoleInfo != null)
            {
                // Mission: backend already computes the correct mission for this user
                missionName = userRoleInfo.MissionName?.Trim();

                // Roles: backend already resolves role names (including temp override)

                var allRoles = userRoleInfo.Roles
                    .Where(r => !string.IsNullOrWhiteSpace(r))
                    .Distinct()
                    .ToList();

                rolesText = string.Join(", ", allRoles);
            }

            identity.AddClaim(new Claim(
                type: "Mission",
                value: missionName,
                valueType: ClaimValueTypes.String));

            identity.AddClaim(new Claim(
                type: "AllRoles",
                value: rolesText,
                valueType: ClaimValueTypes.String));


            // Add tenant claim
            identity.AddClaim(new Claim(
                type: "Tenant",
                value: TenantId,
                valueType: ClaimValueTypes.String));

            // Add roles
            foreach (var role in roleList)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }

            context.HttpContext.User = context.Principal!;

            Log.Information(
                "OIDC OnTokenValidated complete: UserId={UserId}, TenantClaim={TenantId}, RoleCount={RoleCount}",
                userId, TenantId, roleList.Count);

        };


    })
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

}


static string MaskClientId(string? clientId) =>
    string.IsNullOrEmpty(clientId) ? "(not set)" :
    clientId.Length <= 8 ? clientId : $"{clientId[..8]}...";

static bool IsMultiTenantAuthority(string? tenantId) =>
    tenantId is "common" or "organizations" or "consumers";

static string BuildOidcAuthority(string instance, string tenantId)
{
    var baseUrl = instance.EndsWith('/') ? instance : instance + "/";
    return $"{baseUrl}{tenantId}/v2.0";
}


// Register BearerTokenHandler for HttpClient
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BearerTokenHandler>();

// Configure HttpClient with DevHeaderHandler
builder.Services.AddHttpClient<IRestApiService, ManagementApiService>(client =>
{
    var raw = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Api.Management_Api)
                        ?? "";

    if (!string.IsNullOrEmpty(raw))
    {
        if (!raw.EndsWith("/")) raw += "/";
        client.BaseAddress = new Uri(raw, UriKind.Absolute);
    }
}).AddHttpMessageHandler<BearerTokenHandler>();


builder.Services.AddControllersWithViews();

builder.Services.AddAntiforgery(options =>
{
    // Use non-default token names to avoid framework disclosure findings.
    options.FormFieldName = "_bravePortalCsrf";
    options.HeaderName = "X-BRaVe-Portal-CSRF";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});


builder.Services.AddSingleton<ISecretProvider, EnvSecretProvider>();
builder.Services.AddSingleton<ILookupService, LookupService>();
builder.Services.AddSingleton<IRBAHelperService, RiskBenefitAssessmentService>();
builder.Services.AddSingleton<IAccessControlClient, AccessControlClient>();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var secretProvider = sp.GetRequiredService<ISecretProvider>();
    try
    {
        var cs = secretProvider.GetSecretAsync(KeyVaultSecretNames.Storage.RedisConnection).GetAwaiter().GetResult();
        if (string.IsNullOrWhiteSpace(cs))
        {
            Log.Warning("Redis connection string is not configured.");
            throw new InvalidOperationException("Redis connection string is not configured.");
        }

        var opts = ConfigurationOptions.Parse(cs);
        opts.AbortOnConnectFail = false;
        return ConnectionMultiplexer.Connect(opts);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to connect to Redis at startup.");
        throw;
    }
});

builder.Services.AddSingleton<IAppCache>(sp =>
    new RedisCache(sp.GetRequiredService<IConnectionMultiplexer>(),
                   new RedisCacheOptions { KeyPrefix = "brave:" }));

var coreFields = CoreFieldHelper.BuildCoreFields<Individual>();
builder.Services.AddScoped<IQuestionValidator>(_ =>
    new ExpressionValidatorService(coreFields));

// =====================================
// Blob Storage (Mock – until Azure is provisioned)
// =====================================
//builder.Services.AddSingleton<IBlobStorageService, InMemoryBlobStorage>();
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
builder.Services.AddScoped<TargetingValidationService>();


builder.Services.AddRazorPages();

builder.Services.AddHttpsRedirection(options => options.HttpsPort = 443);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Must run before any middleware that uses scheme/host (HTTPS redirect, cookies, redirects).
app.UseForwardedHeaders();


var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("fr")
    // add more as needed
};

var defaultCulture = "en";

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

// Choose which providers determine culture (order matters)
localizationOptions.RequestCultureProviders = new IRequestCultureProvider[]
{
    new QueryStringRequestCultureProvider(),   // ?culture=fr
    new CookieRequestCultureProvider(),        // .AspNetCore.Culture cookie
    new AcceptLanguageHeaderRequestCultureProvider()
};

app.UseRequestLocalization(localizationOptions);


// ===== Serilog request logging =====
app.UseSerilogRequestLogging();

// ===== HTTP pipeline =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();

            if (feature != null)
            {
                Log.Error(feature.Error, "Unhandled exception occurred");
            }

            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("An unexpected error occurred.");
        });
    });
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode >= 500)
    {
        Log.Error("HTTP {StatusCode} for {Method} {Path}{Query}",
            context.Response.StatusCode,
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString);
    }
});

// Must run before Map* so it wraps endpoint execution. Never redirect or set headers if the response has started.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (RedirectToLoginException ex)
    {
        if (context.Response.HasStarted)
        {
            Log.Warning(ex, "RedirectToLoginException after response started; cannot redirect to /logout.");
            return;
        }

        context.Response.Redirect("/logout");
    }
});

app.MapControllers();
app.MapRazorPages();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    environment = appEnvironment,
    hostname = websiteHostname,
    formOnlyAuth = isFormOnlyAuth
})).AllowAnonymous();

app.Run();
