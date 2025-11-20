using System;
using System.Text;
using Binah.API.Configuration;
using Binah.API.Hubs;
using Binah.API.Middleware;
using Binah.Core.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithThreadId()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting Binah API Gateway");

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Binah API Gateway", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new()
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new()
        {
            {
                new()
                {
                    Reference = new()
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Configure JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("Authentication:Jwt");
    var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key is not configured");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };

        // Configure for SignalR WebSocket authentication
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("ReadAccess", policy =>
            policy.RequireAuthenticatedUser());

        options.AddPolicy("WriteAccess", policy =>
            policy.RequireAuthenticatedUser());

        options.AddPolicy("AdminOnly", policy =>
            policy.RequireRole("Admin"));
    });

    // Configure HTTP clients with Polly resilience
    builder.Services.AddBinahHttpClients(builder.Configuration);

    // Add Domain Registry HTTP client
    builder.Services.AddHttpClient("DomainRegistry", client =>
    {
        var domainRegistryUrl = builder.Configuration["DomainRegistry:Url"] ?? "http://localhost:8095";
        client.BaseAddress = new Uri(domainRegistryUrl);
        client.Timeout = TimeSpan.FromSeconds(5);
    });

    // Add memory cache for domain validation caching
    builder.Services.AddMemoryCache();

    // Configure API versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    });

    // Configure CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:3000" };

            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    // Add response compression
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    // Add SignalR for real-time communication
    builder.Services.AddSignalR();

    // Register real-time notification service
    builder.Services.AddSingleton<IRealtimeNotificationService, RealtimeNotificationService>();

    // Add health checks with downstream service checks
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy("API Gateway is running"))
        .AddCheck("downstream-ontology", () =>
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var response = client.GetAsync(builder.Configuration["Services:Ontology:Url"] + "/health").Result;
                return response.IsSuccessStatusCode
                    ? HealthCheckResult.Healthy("Ontology service is accessible")
                    : HealthCheckResult.Degraded($"Ontology service returned {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Degraded($"Ontology service unreachable: {ex.Message}");
            }
        }, tags: new[] { "downstream", "ontology" })
        .AddCheck("downstream-auth", () =>
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var response = client.GetAsync(builder.Configuration["Services:Auth:Url"] + "/health").Result;
                return response.IsSuccessStatusCode
                    ? HealthCheckResult.Healthy("Auth service is accessible")
                    : HealthCheckResult.Degraded($"Auth service returned {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Degraded($"Auth service unreachable: {ex.Message}");
            }
        }, tags: new[] { "downstream", "auth" });

    // Configure Kestrel to listen on port 8092
    builder.WebHost.UseUrls("http://0.0.0.0:8092");

    var app = builder.Build();

    // Configure middleware pipeline

    // Use correlation ID tracking
    app.UseMiddleware<CorrelationIdMiddleware>();

    // Use exception handling
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // Use rate limiting
    var requestsPerMinute = builder.Configuration.GetValue<int>("RateLimit:RequestsPerMinute", 100);
    app.UseRateLimiting(requestsPerMinute);

    // Use response compression
    app.UseResponseCompression();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Use HTTPS redirection
    app.UseHttpsRedirection();

    // Use CORS
    app.UseCors("AllowAll");

    // Use domain context middleware (extract and validate domain from request)
    app.UseDomainContext();

    // Use routing
    app.UseRouting();

    // Use authentication and authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Map controllers
    app.MapControllers();

    // Map SignalR hubs
    app.MapHub<RealtimeHub>("/hubs/realtime");

    // Root endpoint
    app.MapGet("/", () => new
    {
        service = "Binah API Gateway",
        version = "1.0.0",
        status = "healthy",
        timestamp = DateTime.UtcNow
    });

    // Prometheus metrics
    app.UseMetricServer();    // Exposes /metrics endpoint
    app.UseHttpMetrics();     // Records HTTP request metrics

    Log.Information("Binah API Gateway started successfully on {Environment}", app.Environment.EnvironmentName);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Binah API Gateway terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program accessible to integration tests
namespace Binah.API
{
    public partial class Program { }
}
