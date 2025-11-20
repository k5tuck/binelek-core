using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Neo4j.Driver;
using Confluent.Kafka;
using Binah.Ontology.Data;
using Binah.Ontology.Services;
using Binah.Ontology.Repositories;
using Binah.Ontology.HealthChecks;
using Binah.Ontology.Models.Exceptions;
// using Binah.Ontology.GraphQL;  // TODO: Uncomment when GraphQL is available
// using Binah.Ontology.Middleware;  // TODO: Uncomment when Middleware is available
using Binah.Core.Exceptions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HotChocolate;
using HotChocolate.AspNetCore;
using Serilog;
using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Prometheus;

namespace Binah.Ontology
{
// Converted from file-scoped namespace to allow exceptions namespace below

public class Program
{
    public static void Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build())
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", "Binah.Ontology")
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Starting Binah.Ontology service");
            var app = CreateHostBuilder(args);

            // Initialize database schema synchronously before starting the server
            InitializeDatabaseAsync(app.Services).GetAwaiter().GetResult();

            Log.Information("Database initialization complete, starting HTTP server...");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application start-up failed");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static WebApplication CreateHostBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add Serilog
        builder.Host.UseSerilog();

        // Configuration
        var configuration = builder.Configuration;

        // === NEO4J CONFIGURATION ===
        var neo4jUri = configuration["Neo4j:Uri"] 
            ?? throw new OntologyConfigurationException("Neo4j:Uri configuration is missing");
        var neo4jUser = configuration["Neo4j:Username"] 
            ?? throw new OntologyConfigurationException("Neo4j:Username configuration is missing");
        var neo4jPassword = configuration["Neo4j:Password"] 
            ?? throw new OntologyConfigurationException("Neo4j:Password configuration is missing");
        var neo4jDatabase = configuration["Neo4j:Database"] ?? "Binah";

        builder.Services.AddSingleton<IDriver>(sp =>
        {
            try
            {
                var driver = GraphDatabase.Driver(
                    neo4jUri,
                    AuthTokens.Basic(neo4jUser, neo4jPassword),
                    config => config
                        .WithMaxConnectionLifetime(TimeSpan.FromMinutes(30))
                        .WithMaxConnectionPoolSize(50)
                        .WithConnectionAcquisitionTimeout(TimeSpan.FromSeconds(30))
                        .WithConnectionTimeout(TimeSpan.FromSeconds(15))
                        .WithEncryptionLevel(EncryptionLevel.None) // Use Encrypted in production
                        .WithTrustManager(TrustManager.CreateInsecure()) // Use proper certs in production
                );

                // Verify connectivity
                driver.VerifyConnectivityAsync().GetAwaiter().GetResult();
                Log.Information("Neo4j connection established successfully");

                return driver;
            }
            catch (Exception ex)
            {
                throw new OntologyConnectionException("Failed to connect to Neo4j database", ex);
            }
        });

        // === DATA NETWORK NEO4J CONFIGURATION ===
        var neo4jDataNetworkUri = configuration["Neo4jDataNetwork:Uri"] ?? neo4jUri;
        var neo4jDataNetworkUser = configuration["Neo4jDataNetwork:Username"] ?? neo4jUser;
        var neo4jDataNetworkPassword = configuration["Neo4jDataNetwork:Password"] ?? neo4jPassword;
        var neo4jDataNetworkDatabase = configuration["Neo4jDataNetwork:Database"] ?? "data-network";

        builder.Services.AddSingleton<Binah.Ontology.Infrastructure.IDataNetworkNeo4jDriver>(sp =>
        {
            try
            {
                var driver = GraphDatabase.Driver(
                    neo4jDataNetworkUri,
                    AuthTokens.Basic(neo4jDataNetworkUser, neo4jDataNetworkPassword),
                    config => config
                        .WithMaxConnectionLifetime(TimeSpan.FromMinutes(30))
                        .WithMaxConnectionPoolSize(25) // Smaller pool for data network
                        .WithConnectionAcquisitionTimeout(TimeSpan.FromSeconds(30))
                        .WithConnectionTimeout(TimeSpan.FromSeconds(15))
                        .WithEncryptionLevel(EncryptionLevel.None)
                        .WithTrustManager(TrustManager.CreateInsecure())
                );

                // Verify connectivity
                driver.VerifyConnectivityAsync().GetAwaiter().GetResult();
                Log.Information("Data Network Neo4j connection established successfully at {Uri}", neo4jDataNetworkUri);

                return new Binah.Ontology.Infrastructure.DataNetworkNeo4jDriver(driver);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to connect to Data Network Neo4j at {Uri}. Data network contribution will be disabled.", neo4jDataNetworkUri);
                // Return null to allow service to start without data network (optional feature)
                return null!;
            }
        });

        // === KAFKA CONFIGURATION ===
        var kafkaBrokers = configuration["Kafka:BootstrapServers"] 
            ?? throw new OntologyConfigurationException("Kafka:BootstrapServers configuration is missing");

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaBrokers,
            ClientId = "Binah-ontology-producer",
            Acks = Acks.All, // Required when EnableIdempotence = true
            MessageTimeoutMs = 10000,
            RequestTimeoutMs = 30000,
            EnableIdempotence = true,
            MaxInFlight = 5,
            CompressionType = CompressionType.Snappy
        };

        builder.Services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var producer = new ProducerBuilder<string, string>(producerConfig).Build();
            Log.Information("Kafka producer initialized successfully");
            return producer;
        });

        // === REGISTER SERVICES ===

        // === DATABASE CONFIGURATION (for ontology version management) ===
        // Using PostgreSQL
        var postgresConnectionString = configuration.GetConnectionString("OntologyDb")
            ?? "Host=localhost;Port=5432;Database=binah_ontology;Username=postgres;Password=postgres";

        builder.Services.AddDbContext<OntologyDbContext>(options =>
        {
            options.UseNpgsql(postgresConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                npgsqlOptions.CommandTimeout(30);
            });

            if (builder.Environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Register Neo4j database name for services
        builder.Services.AddSingleton(sp => neo4jDatabase);

        // TODO: Uncomment when IGraphDatabaseService is available
        // builder.Services.AddSingleton<IGraphDatabaseService>(sp =>
        //     new GraphDatabaseService(
        //         sp.GetRequiredService<IDriver>(),
        //         neo4jDatabase,
        //         sp.GetRequiredService<ILogger<GraphDatabaseService>>()
        //     )
        // );

        builder.Services.AddSingleton<IEntityRepository, EntityRepository>();
        builder.Services.AddSingleton<IRelationshipRepository, RelationshipRepository>();
        builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        // === DATA NETWORK SERVICES ===
        builder.Services.AddScoped<Binah.Ontology.Repositories.Interfaces.ITenantRepository, Binah.Ontology.Repositories.Implementations.TenantRepository>();
        builder.Services.AddSingleton<Binah.Ontology.Pipelines.DataNetwork.IDataNetworkStore, Binah.Ontology.Repositories.DataNetworkStore>();
        builder.Services.AddScoped<Binah.Ontology.Pipelines.DataNetwork.ITenantConsentValidator, Binah.Ontology.Services.TenantConsentValidator>();
        
        // Domain-agnostic PII scrubber
        builder.Services.AddSingleton<Binah.Ontology.Pipelines.DataNetwork.IPiiScrubber, Binah.Ontology.Pipelines.DataNetwork.PiiScrubber>();
        
        // Domain-agnostic data network pipeline (works with any domain)
        builder.Services.AddScoped<Binah.Ontology.Pipelines.DataNetwork.IDataNetworkPipeline, Binah.Ontology.Pipelines.DataNetwork.DataNetworkPipeline>();

        builder.Services.AddScoped<IEntityService, EntityService>();
        builder.Services.AddScoped<IRelationshipService, RelationshipService>();
        builder.Services.AddScoped<IQueryService, QueryService>();
        builder.Services.AddScoped<ILineageService, LineageService>();
        builder.Services.AddScoped<IOntologyVersionService, OntologyVersionService>();
        builder.Services.AddScoped<IExportService, ExportService>();

        // === CANVAS SERVICE ===
        builder.Services.AddScoped<ICanvasRepository, CanvasRepository>();

        // === SCHEMA INTROSPECTION SERVICE (Ontology-First Architecture) ===
        builder.Services.AddScoped<Binah.Ontology.Services.Interfaces.ISchemaMetadataService, Binah.Ontology.Services.Implementations.SchemaMetadataService>();
        builder.Services.AddScoped<Binah.Ontology.Services.ISchemaChangePublisher, Binah.Ontology.Services.SchemaChangePublisher>();

        // === ENRICHMENT AND CLASSIFICATION SERVICES ===
        builder.Services.AddScoped<IEnrichmentService, EnrichmentService>();
        builder.Services.AddScoped<IRelationshipInferenceService, RelationshipInferenceService>();
        builder.Services.AddScoped<IClassificationService, ClassificationService>();

        // === DATA NETWORK ANALYTICS SERVICE ===
        builder.Services.AddScoped<IDataNetworkService, DataNetworkService>();

        // === ACTIONS AND WATCHES SERVICES ===
        builder.Services.AddScoped<IActionRepository, ActionRepository>();
        builder.Services.AddScoped<IActionService, ActionService>();
        builder.Services.AddScoped<IWatchRepository, WatchRepository>();
        builder.Services.AddScoped<IWatchService, WatchService>();

        // === PATTERN TEMPLATES SERVICES ===
        builder.Services.AddScoped<IPatternTemplateRepository, PatternTemplateRepository>();
        builder.Services.AddScoped<IPatternTemplateService, PatternTemplateService>();

        // Register Neo4j Repository for entity storage
        builder.Services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Binah.Ontology.Repositories.Neo4jRepository>>();
            return new Binah.Ontology.Repositories.Neo4jRepository(neo4jUri, neo4jUser, neo4jPassword, logger);
        });

        // Register Entity Validation Background Service (consumes from Kafka, validates, stores in Neo4j)
        // Temporarily disabled - requires Kafka to be available
        // builder.Services.AddHostedService<Binah.Ontology.Services.EntityValidationService>();

        // === KAFKA CONSUMERS (BACKGROUND SERVICES) ===
        // Register Kafka consumers as hosted services
        builder.Services.AddHostedService<Binah.Ontology.Consumers.EnrichmentRequestConsumer>();
        builder.Services.AddHostedService<Binah.Ontology.Consumers.PipelineCompletionConsumer>();

        // === GRAPHQL CONFIGURATION ===
        // TODO: Uncomment when GraphQL types are available
        // builder.Services
        //     .AddGraphQLServer()
        //     .AddQueryType<QueryType>()
        //     .AddMutationType<MutationType>()
        //     .AddSubscriptionType<SubscriptionType>()
        //     .AddType<ProjectType>()
        //     .AddType<ContractorType>()
        //     .AddType<PropertyType>()
        //     .AddType<InvestorType>()
        //     .AddType<TransactionType>()
        //     .AddFiltering()
        //     .AddSorting()
        //     .AddProjections()
        //     .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment())
        //     .AddInMemorySubscriptions();

        // === HTTP SERVICES ===
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Binah Ontology API",
                Version = "v1",
                Description = "Graph-based entity modeling and relationship management for Binah",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "BuildLedger Team",
                    Email = "support@buildledger.com"
                }
            });
        });

        // === JWT AUTHENTICATION ===
        var jwtSettings = configuration.GetSection("Authentication:Jwt");
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
        });

        builder.Services.AddAuthorization();

        // === CORS ===
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // === HEALTH CHECKS ===
        builder.Services
            .AddHealthChecks()
            .AddCheck("neo4j-production", () =>
            {
                try
                {
                    using var driver = Neo4j.Driver.GraphDatabase.Driver(
                        neo4jUri,
                        AuthTokens.Basic(neo4jUser, neo4jPassword));
                    using var session = driver.AsyncSession();
                    session.RunAsync("RETURN 1").GetAwaiter().GetResult();
                    return HealthCheckResult.Healthy("Neo4j is accessible");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy($"Neo4j unreachable: {ex.Message}");
                }
            }, tags: new[] { "neo4j", "production", "critical" })
            .AddCheck("postgresql", () =>
            {
                try
                {
                    using var conn = new Npgsql.NpgsqlConnection(postgresConnectionString);
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
            }, tags: new[] { "database", "postgresql", "critical" })
            .AddCheck("kafka-producer", () =>
            {
                try
                {
                    // Simple check - just verify config is present
                    var bootstrapServers = builder.Configuration["Kafka:BootstrapServers"];
                    return string.IsNullOrEmpty(bootstrapServers)
                        ? HealthCheckResult.Unhealthy("Kafka configuration missing")
                        : HealthCheckResult.Healthy($"Kafka configured: {bootstrapServers}");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy($"Kafka producer failed: {ex.Message}");
                }
            }, tags: new[] { "kafka", "critical" })
            .AddCheck("data-network-neo4j", () =>
            {
                try
                {
                    var dataNetworkDriver = builder.Services.BuildServiceProvider().GetService<Binah.Ontology.Infrastructure.IDataNetworkNeo4jDriver>();
                    if (dataNetworkDriver == null)
                    {
                        return HealthCheckResult.Degraded("Data Network Neo4j not configured (optional feature)");
                    }
                    return HealthCheckResult.Healthy("Data Network Neo4j accessible");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Degraded($"Data Network unavailable: {ex.Message}");
                }
            }, tags: new[] { "neo4j", "data-network", "optional" });

        // === CONFIGURE PORT ===
        // Port configured via ASPNETCORE_URLS environment variable from docker-compose
        // builder.WebHost.UseUrls("http://0.0.0.0:8091");

        // === BUILD APPLICATION ===
        var app = builder.Build();

        // === PROMETHEUS METRICS ===
        app.UseMetricServer();    // Exposes /metrics endpoint
        app.UseHttpMetrics();     // Records HTTP request metrics

        // === MIDDLEWARE PIPELINE ===
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Binah Ontology API v1");
                c.RoutePrefix = string.Empty;
            });
        }

        // Use custom middleware - FIRST before authentication
        app.UseMiddleware<Binah.Core.Middleware.CorrelationIdMiddleware>();
        app.UseMiddleware<Binah.Core.Middleware.ExceptionHandlingMiddleware>();

        app.UseSerilogRequestLogging();
        app.UseRouting();
        app.UseCors();

        // CRITICAL: Order matters - Authentication before Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseWebSockets();

        app.MapControllers();
        // TODO: Uncomment when GraphQL is available
        // app.MapGraphQL("/graphql");

        // Health check endpoints are handled by HealthController
        // (GET /health, GET /health/ready, GET /health/live)

        // TODO: Uncomment when GraphQL is available
        // app.MapGet("/", () => Results.Redirect("/graphql"));

        return app;
    }

    public static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        // === POSTGRESQL MIGRATIONS ===
        try
        {
            Log.Information("Applying PostgreSQL migrations...");
            var dbContext = scope.ServiceProvider.GetRequiredService<OntologyDbContext>();
            await dbContext.Database.MigrateAsync();
            Log.Information("PostgreSQL migrations applied successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to apply PostgreSQL migrations - database may not be available. Service will continue without persistent ontology versioning.");
            // Don't throw - allow service to continue running even without database
        }

        // === NEO4J SCHEMA INITIALIZATION ===
        // TODO: Uncomment when IGraphDatabaseService is available
        // var dbService = scope.ServiceProvider.GetRequiredService<IGraphDatabaseService>();

        // try
        // {
        //     Log.Information("Initializing Neo4j database schema...");

        //     // Create constraints
        //     await dbService.ExecuteCypherAsync(@"
        //         CREATE CONSTRAINT project_id IF NOT EXISTS
        //         FOR (p:Project) REQUIRE p.id IS UNIQUE
        //     ");
        //
        //     await dbService.ExecuteCypherAsync(@"
        //         CREATE CONSTRAINT contractor_id IF NOT EXISTS
        //         FOR (c:Contractor) REQUIRE c.id IS UNIQUE
        //     ");
        //
        //     await dbService.ExecuteCypherAsync(@"
        //         CREATE CONSTRAINT property_id IF NOT EXISTS
        //         FOR (p:Property) REQUIRE p.id IS UNIQUE
        //     ");
        //
        //     await dbService.ExecuteCypherAsync(@"
        //         CREATE CONSTRAINT investor_id IF NOT EXISTS
        //         FOR (i:Investor) REQUIRE i.id IS UNIQUE
        //     ");
        //
        //     await dbService.ExecuteCypherAsync(@"
        //         CREATE CONSTRAINT transaction_id IF NOT EXISTS
        //         FOR (t:Transaction) REQUIRE t.id IS UNIQUE
        //     ");

        //     // Create indexes for common queries
        //     await dbService.ExecuteCypherAsync(@"
        //         CREATE INDEX project_status IF NOT EXISTS
        //         FOR (p:Project) ON (p.status)
        //     ");
        //
        //     await dbService.ExecuteCypherAsync(@"
        //         CREATE INDEX project_name IF NOT EXISTS
        //         FOR (p:Project) ON (p.name)
        //     ");

        //     await dbService.ExecuteCypherAsync(@"
        //         CREATE FULLTEXT INDEX entity_search IF NOT EXISTS
        //         FOR (n:Project|Contractor|Property|Investor)
        //         ON EACH [n.name, n.description]
        //     ");

        //     Log.Information("Database schema initialized successfully");
        // }
        // catch (Exception ex)
        // {
        //     Log.Error(ex, "Failed to initialize database schema");
        //     throw new Binah.Core.Exceptions.BinahException("Database schema initialization failed", ex);
        // }
    }
}
} // End Binah.Ontology namespace