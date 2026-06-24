using Azure.Security.KeyVault.Keys;
using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;
using BRaVe_Mobile_Backend.Services;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Serilog;

//using Serilog;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

var cfg = builder.Configuration;

var aiConnectionString = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Logging.App_Insight);

string connectionString = Environment.GetEnvironmentVariable(KeyVaultSecretNames.SBQ.ServiceBusConnection);
string queueName = Environment.GetEnvironmentVariable(KeyVaultSecretNames.SBQ.ServiceBusQueueName);


builder.Services.AddSingleton<IServiceBusSender>(sp =>
    new ServiceBusSender(connectionString, queueName));


Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .WriteTo.ApplicationInsights(
        new TelemetryConfiguration
        {
            ConnectionString = aiConnectionString
        },
        TelemetryConverter.Traces)
    .CreateLogger();

builder.Host.UseSerilog();



// Add services to the container.
builder.Services.AddSingleton<JwsSignerService>();

var useMock = builder.Environment.IsDevelopment();/*&&
              string.Equals(cfg["Auth:Mode"], "Mock", StringComparison.OrdinalIgnoreCase);*/

IAuthModeService authModeService = new AuthModeService(useMock);

builder.Services.AddSingleton(authModeService);

//builder.Services.AddSingleton<IAuthModeService, AuthModeService>();

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<QueuedHostedService>();
builder.Services.AddScoped<IDataLoaderService, SqlDataLoaderService>();


builder.Services.AddSingleton<IRegistrationActivityService,SqlRegistrationActivityService>();
builder.Services.AddSingleton<ISecretProvider, EnvSecretProvider>();
builder.Services.AddSingleton<AzureKeyVaultHelper>();
builder.Services.AddSingleton<INonceService, SqlNonceService>();
builder.Services.AddScoped<IClaimService, SqlClaimService>();
builder.Services.AddScoped<IPubService, SqlPubService>();
builder.Services.AddScoped<IRefreshTokenService, SqlRefreshTokenService>();


builder.Services.AddSingleton<IEnumeratorService, SqlEnumeratorService>();
builder.Services.AddSingleton<IDeviceService, SqlDeviceProfileService>();
builder.Services.AddSingleton<IRequestContextValidator, RequestContextValidator>();


//builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Debug);
//builder.Logging.AddFilter("Microsoft.IdentityModel", LogLevel.Debug);

// 1) Configure which headers to trust
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Optional but common when you're on Azure / behind a LB and don't want to hardcode proxies:
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Build service provider to resolve JwsSignerService
    var serviceProvider = builder.Services.BuildServiceProvider();
    //var signerService = serviceProvider.GetRequiredService<JwsSignerService>();
    var kvhelper = serviceProvider.GetRequiredService<AzureKeyVaultHelper>();

    KeyVaultKey key = kvhelper.GetKeyAsync().Result;

    var rsaParams = new RSAParameters
    {
        Modulus = key.Key.N,
        Exponent = key.Key.E
    };

    var audiences = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Jwt.Enroll_Audience);
    var issuer = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Jwt.Issuer);


    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer, //"https://auth.brave.iom.int", // matches your "iss" claim

        ValidateAudience = true,
        ValidAudiences = audiences.Split(","),//["brave-enroll", "brave-api"], // matches your "aud" claim

        ValidateLifetime = false,
       // ClockSkew = TimeSpan.Zero, // optional, removes default 5-min skew

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new RsaSecurityKey(rsaParams), // see below
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT Auth Failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Console.WriteLine($"JWT Challenge: {context.Error}, {context.ErrorDescription}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
