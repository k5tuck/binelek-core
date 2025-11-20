using Binah.Auth.Models;
using Binah.Auth.Repositories;
using Binah.Auth.Services;
using Binah.Core.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prometheus;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Binah Authentication API",
        Version = "v1",
        Description = "Authentication and authorization service for the Binah platform"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure Database
// Enable legacy timestamp behavior to allow DateTime values without explicit UTC kind
// This is required for Npgsql 8.0+ compatibility with existing code
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("AuthDatabase"));
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(dataSource));

// Configure JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);

var secret = jwtSettings.GetValue<string>("Secret") ?? throw new InvalidOperationException("JWT Secret is not configured");
var key = Encoding.UTF8.GetBytes(secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
        ValidateAudience = true,
        ValidAudience = jwtSettings.GetValue<string>("Audience"),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();

// Register services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOAuthService, OAuthService>();
builder.Services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
builder.Services.AddScoped<Binah.Core.Services.IAuditService, AuditService>();
// Use SAML service (SamlServiceProduction excluded due to API version mismatch)
builder.Services.AddScoped<ISamlService, SamlService>();
// Email service for verification and notifications
builder.Services.AddScoped<IEmailService, EmailService>();
// Webhooks moved to separate binah-webhooks service
// builder.Services.AddScoped<IWebhookService, WebhookService>();
// builder.Services.AddScoped<IWebhookDeliveryService, WebhookDeliveryService>();

// Team Workspace services
builder.Services.AddScoped<ITeamService, TeamService>();

// API Key management services
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();

// SSO Configuration services
builder.Services.AddScoped<ISsoConfigService, SsoConfigService>();

// Add HTTP client for webhooks
builder.Services.AddHttpClient("webhook");

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("postgresql", () =>
    {
        try
        {
            var connectionString = builder.Configuration.GetConnectionString("AuthDatabase")
                ?? throw new InvalidOperationException("AuthDatabase connection string is not configured");
            using var conn = new Npgsql.NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            cmd.ExecuteScalar();
            return HealthCheckResult.Healthy("PostgreSQL is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"PostgreSQL unreachable: {ex.Message}");
        }
    }, tags: new[] { "db", "sql" })
    .AddCheck("jwt-signing-key", () =>
    {
        var key = builder.Configuration["JwtSettings:Secret"];
        return string.IsNullOrEmpty(key) || key.Length < 32
            ? HealthCheckResult.Unhealthy("JWT signing key too short or missing")
            : HealthCheckResult.Healthy("JWT signing key configured");
    });

// Configure Kestrel to listen on port 8093
builder.WebHost.UseUrls("http://0.0.0.0:8093");

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use custom middleware
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestTimingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<Binah.Core.Middleware.AuditMiddleware>();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics();

// Database migration
// NOTE: Schema is auto-created by init-postgres.sql in Docker
// If you need to add new EF Core migrations, uncomment the Migrate() line below
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    try
    {
        // Ensure database exists and can connect
        if (db.Database.CanConnect())
        {
            Log.Information("Database connection successful");
            // Uncomment to run EF Core migrations:
            // db.Database.Migrate();
        }
        else
        {
            Log.Warning("Cannot connect to database");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while connecting to the database");
    }
}

Log.Information("Starting Binah.Auth service on port 8093");

app.Run();

// Make Program accessible to integration tests
namespace Binah.Auth
{
    public partial class Program { }
}
