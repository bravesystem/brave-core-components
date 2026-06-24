using Azure.Security.KeyVault.Keys;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Helpers.Mock;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Interfaces.jobs;
using BRaVe_Management_Backend.Interfaces.uat_login;
using BRaVe_Management_Backend.Services;
using BRaVe_Management_Backend.Services.jobs;
using BRaVe_Management_Backend.Services.Mock;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;


var builder = WebApplication.CreateBuilder(args);

var cfg = builder.Configuration;

var aiConnectionString = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Logging.App_Insight);// ?? cfg["ApplicationInsights:ConnectionString"];

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

bool devMock = builder.Environment.IsDevelopment();

/*&& string.Equals(cfg["Auth:Mode"], "Mock", StringComparison.OrdinalIgnoreCase);*/

IAuthModeService authModeService = new AuthModeService(devMock);
builder.Services.AddSingleton(authModeService);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "ApiAuth";
        options.DefaultChallengeScheme = "ApiAuth";
    })
    .AddPolicyScheme("ApiAuth", "AzureAD or FieldDevice JWT", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var auth = context.Request.Headers.Authorization.ToString();
            if (!auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return "AzureAdApiAuth";

            var token = auth["Bearer ".Length..].Trim();
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var iss = jwt.Issuer ?? "";

            // Route by issuer
            if (iss.Contains("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase)
                || iss.Contains("sts.windows.net", StringComparison.OrdinalIgnoreCase))
                return "AzureAdApiAuth";

            return "FieldDeviceJwt";
        };
    })
    .AddScheme<AuthenticationSchemeOptions, AzureAdApiAuthHandler>("AzureAdApiAuth", _ => { })
    .AddJwtBearer("FieldDeviceJwt", options =>
    {
        var serviceProvider = builder.Services.BuildServiceProvider();
        
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
            ValidIssuer = issuer, 

            ValidateAudience = true,
            ValidAudiences = audiences.Split(","),

            ValidateLifetime = false,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsaParams),
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

builder.Services.AddSingleton<IRoleService, SqlRoleService>();

builder.Services.AddScoped<JwsSignerService>();

builder.Services.AddSingleton(new AppEnrollmentOptions
{
    ApiBaseUrl = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Jwt.API_BASE_URL)

});

builder.Services.AddScoped<IAttestationVerifier, MockAttestationVerifier>(); //to replace

builder.Services.AddSingleton<ISecretProvider, EnvSecretProvider>();


builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var secretProvider = sp.GetRequiredService<ISecretProvider>();
    var cs = secretProvider.GetSecretAsync(KeyVaultSecretNames.Storage.RedisConnection).Result;
    var opts = ConfigurationOptions.Parse(cs);
    opts.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(opts);
});

builder.Services.AddSingleton<IAppCache>(sp =>
    new RedisCache(sp.GetRequiredService<IConnectionMultiplexer>(),
                   new RedisCacheOptions { KeyPrefix = "brave:" }));


builder.Services.AddSingleton<AzureKeyVaultHelper>();

builder.Services.AddSingleton<IUatFormAccessService, SqlUatFormAccessService>();
builder.Services.AddSingleton<IIndicatorRepositoryService, SqlIndicatorService>();

builder.Services.AddScoped<IDashboardService, SqlDashboardService>();

builder.Services.AddSingleton<IScoringEngine, ImprovedScoringEngineService>();
builder.Services.AddSingleton<ISearchService, SearchEngineService>();

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddScoped<IDataLoaderService, SqlDataLoaderService>();
builder.Services.AddHostedService<QueuedHostedService>();

builder.Services.AddScoped<IClaimService, SqlClaimService>();
builder.Services.AddScoped<ILookupService,SqlLookupService>();
builder.Services.AddScoped<IRegionService,SqlRegionService>();
builder.Services.AddScoped<IMissionService,SqlMissionService>();
builder.Services.AddScoped<IProgramService,SqlProgramService>();
builder.Services.AddScoped<ISurveyService,SqlSurveyServices>();
builder.Services.AddScoped<IPreferenceService, SqlPreferencesServices>();
builder.Services.AddScoped<IDatapointService, SqlDataPointServices>();
builder.Services.AddScoped<IActivityService, SqlActivitiesServices > ();
builder.Services.AddScoped<IConsentService, SqlConsentService> ();
builder.Services.AddSingleton<IUserMissionMappingService, SqlUserMissionRequestService>();
builder.Services.AddScoped<IRiskBenefitAssessment,SqlRiskBenefitAssessmentService>();

builder.Services.AddScoped<IFlaggedBeneficiariesService, SqlFlaggedBeneficiariesService>();
builder.Services.AddScoped<IEnumeratorService, SqlEnumeratorService>();
builder.Services.AddScoped<IClaimSessionService, SqlClaimSessionService>();

builder.Services.AddScoped<IAssessmentComponentService, SqlAssessmentComponentService>();
builder.Services.AddScoped<ICoMDecisionService, SqlCoMDecisionService>();
builder.Services.AddScoped<IPmDecisionService, SqlPmDecisionService>();
builder.Services.AddScoped<IRecommendationService, SqlRecommendationService>();
builder.Services.AddScoped<ISurveyQuestionService, SqlSurveyQuestionService>();
builder.Services.AddScoped<IAdministrativeLevelService, SqlAdministrativeLevelService>();
builder.Services.AddScoped<ILocationService, SqlLocationService>();
//builder.Services.AddScoped<IConsentService, SqlConsentService>();
builder.Services.AddScoped<IDatasetService, SqlDataSetService>();

builder.Services.AddScoped<IDistributionKitTypeService, SqlDistributionKitTypeService>();


builder.Services.AddScoped<IDistributionAssistanceService, SqlDistributionAssistanceService>();


builder.Services.AddScoped<IDistributionTypesService, SqlDistributionsTypeService>();
builder.Services.AddScoped<IDistributionCompositionService, SqlDistributionsCompositionService>();

builder.Services.AddScoped<ITargetingRulesService, SqlTargetingRulesService>();
builder.Services.AddScoped<ITargetingCriteriaService, SqlTargetingCriteriaService>();
builder.Services.AddScoped<IBeneficiaryEnrollmentsService,
    SqlBeneficiaryEnrollmentsService>();


builder.Services.AddSingleton<IDuplicateScoringEngine, DuplicateScoringEngine>();
builder.Services.AddSingleton<IDeduplicationJobService, SqlDeduplicationJobService>();
builder.Services.AddSingleton<IDuplicateRulesetService, SqlDuplicateRulesetService>();
builder.Services.AddScoped<ISqlBiometricService, SqlBiometricService>();

builder.Services.AddSingleton<IDeduplicationJobService, SqlDeduplicationJobService>();
builder.Services.AddSingleton<ITargetingJob, SqlTargetingJobService>();
builder.Services.AddSingleton<ITargetingJobQueue, TargetingJobQueue>();
builder.Services.AddSingleton<IDeduplicationJobQueue, DeduplicationJobQueue>();
builder.Services.AddScoped<IAdjudicationService, SqlAdjudicationService>();

builder.Services.AddScoped<IMonitoringService, SqlFailedBatchService>();
builder.Services.AddScoped<IDeviceProvisioningMonitoringService,SqlDeviceTenantSwitchService>();

builder.Services.AddScoped<IPrintService, SqlPrintService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

// Composite evaluator
builder.Services.AddSingleton<ICompositeEvaluatorService, CompositeEvaluator>();

// Computation engine
builder.Services.AddSingleton<IIndicatorComputationService, IndicatorComputationService>();

// Targeting job infrastructure
builder.Services.AddHostedService<TargetingJobWorker>();
builder.Services.AddHostedService<DeduplicationJobWorker>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
/*try
{
    var connectionString = Environment.GetEnvironmentVariable(KeyVaultSecretNames.Sql.PrimaryConnection);
    //var connectionString = "Server=NBO-PF53T31J\\SQLEXPRESS;Database=BRaVe-db;Trusted_Connection=True;Encrypt=False;";// builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Connection string 'DefaultConnection' is not set.");
    }

    Console.WriteLine("Starting DB migrations...");
    // DbMigrations.RunMigrations(connectionString);
    Console.WriteLine("DB migrations completed successfully.");
}
catch (Exception ex)
{
    Console.WriteLine("Error running database migrations: " + ex);
    // Stop the app if migrations fail
    Environment.Exit(1);
}*/


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//added for Dev
app.UseRouting();
app.UseAuthentication();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
