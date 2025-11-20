using Binah.Ontology.Models;
using Binah.Ontology.Models.Ontology;
using Binah.Ontology.Models.Tenant;
using Binah.Ontology.Models.Action;
using Binah.Ontology.Models.Watch;
using Binah.Ontology.Models.PatternTemplate;
using Binah.Ontology.Pipelines.DataNetwork;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Binah.Ontology.Data;

/// <summary>
/// SQL Server DbContext for ontology version management
/// Separate from Neo4j graph storage - used for versioning, metadata, and lifecycle management
/// </summary>
public class OntologyDbContext : DbContext
{
    public OntologyDbContext(DbContextOptions<OntologyDbContext> options)
        : base(options)
    {
    }

    public DbSet<OntologyVersion> OntologyVersions { get; set; } = null!;
    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<Canvas> Canvases { get; set; } = null!;

    // Actions
    public DbSet<Models.Action.Action> Actions { get; set; } = null!;
    public DbSet<ActionRun> ActionRuns { get; set; } = null!;

    // Watches
    public DbSet<Watch> Watches { get; set; } = null!;
    public DbSet<WatchEntity> WatchEntities { get; set; } = null!;
    public DbSet<WatchTrigger> WatchTriggers { get; set; } = null!;

    // Pattern Templates
    public DbSet<PatternTemplate> PatternTemplates { get; set; } = null!;
    public DbSet<PatternTemplateRating> PatternTemplateRatings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OntologyVersion>(entity =>
        {
            entity.ToTable("OntologyVersions");

            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => new { e.TenantId, e.IsActive })
                .HasDatabaseName("IX_OntologyVersion_TenantId_IsActive");

            entity.HasIndex(e => new { e.TenantId, e.OntologyName, e.Version })
                .HasDatabaseName("IX_OntologyVersion_TenantId_Name_Version")
                .IsUnique();

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_OntologyVersion_CreatedAt");

            entity.HasIndex(e => e.Branch)
                .HasDatabaseName("IX_OntologyVersion_Branch");

            // Properties
            entity.Property(e => e.OntologyName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Version)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.ModelJson)
                .IsRequired();

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Branch)
                .HasMaxLength(100)
                .IsRequired()
                .HasDefaultValue("main");

            // Configure Id conversion from UUID to string
            entity.Property(e => e.Id)
                .HasConversion(
                    v => Guid.Parse(v),  // Convert string to Guid for database
                    v => v.ToString()     // Convert Guid to string for model
                );

            // Ignore navigation properties and complex types from base Entity class
            // (they're loaded from Neo4j, not stored in SQL Server)
            entity.Ignore(e => e.Entities);
            entity.Ignore(e => e.Relationships);
            entity.Ignore(e => e.Properties);
            entity.Ignore(e => e.Metadata);
            entity.Ignore(e => e.Type);
            entity.Ignore(e => e.Source);
            entity.Ignore(e => e.UpdatedAt);
            entity.Ignore(e => e.UpdatedBy);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.DeletedAt);
            entity.Ignore(e => e.DeletedBy);
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");

            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Tenant_IsActive");

            entity.HasIndex(e => e.DataNetworkConsent)
                .HasDatabaseName("IX_Tenant_DataNetworkConsent")
                .HasFilter("[DataNetworkConsent] = 1");

            // Properties
            entity.Property(e => e.Id)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.DataNetworkConsentVersion)
                .HasMaxLength(10)
                .HasDefaultValue("1.0");

            entity.Property(e => e.PiiScrubbingLevel)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(ScrubbingLevel.Strict);

            // Store DataNetworkCategories as JSON
            entity.Property(e => e.DataNetworkCategories)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                )
                .HasColumnType("nvarchar(max)")
                .HasDefaultValueSql("'[]'");
        });

        modelBuilder.Entity<Canvas>(entity =>
        {
            entity.ToTable("Canvases");

            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_Canvas_TenantId");

            entity.HasIndex(e => new { e.TenantId, e.CreatedBy })
                .HasDatabaseName("IX_Canvas_TenantId_CreatedBy");

            entity.HasIndex(e => e.UpdatedAt)
                .HasDatabaseName("IX_Canvas_UpdatedAt");

            // Properties
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            // Store complex types as JSON
            entity.Property(e => e.Entities)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<CanvasEntity>>(v, (JsonSerializerOptions?)null) ?? new List<CanvasEntity>()
                )
                .HasColumnType("text");

            entity.Property(e => e.Connections)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<CanvasConnection>>(v, (JsonSerializerOptions?)null) ?? new List<CanvasConnection>()
                )
                .HasColumnType("text");

            entity.Property(e => e.Viewport)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<CanvasViewport>(v, (JsonSerializerOptions?)null) ?? new CanvasViewport()
                )
                .HasColumnType("text");

            entity.Property(e => e.SharedWith)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()
                )
                .HasColumnType("text");
        });

        // === ACTIONS CONFIGURATION ===
        modelBuilder.Entity<Models.Action.Action>(entity =>
        {
            entity.ToTable("Actions");

            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_Action_TenantId");

            entity.HasIndex(e => new { e.TenantId, e.Status })
                .HasDatabaseName("IX_Action_TenantId_Status");

            entity.HasIndex(e => e.TriggerType)
                .HasDatabaseName("IX_Action_TriggerType");

            // Properties
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.Schedule)
                .HasMaxLength(100);

            entity.Property(e => e.EventTopic)
                .HasMaxLength(200);

            entity.Property(e => e.Configuration)
                .HasColumnType("text")
                .IsRequired();

            entity.Property(e => e.TargetEntityTypes)
                .HasColumnType("text");

            entity.Property(e => e.ConditionExpression)
                .HasColumnType("text");

            entity.Property(e => e.TenantId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100);

            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100);

            entity.Property(e => e.DeletedBy)
                .HasMaxLength(100);
        });

        modelBuilder.Entity<ActionRun>(entity =>
        {
            entity.ToTable("ActionRuns");

            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => new { e.ActionId, e.TenantId })
                .HasDatabaseName("IX_ActionRun_ActionId_TenantId");

            entity.HasIndex(e => e.StartedAt)
                .HasDatabaseName("IX_ActionRun_StartedAt");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_ActionRun_Status");

            // Properties
            entity.Property(e => e.TriggeredBy)
                .HasMaxLength(100);

            entity.Property(e => e.InputData)
                .HasColumnType("text");

            entity.Property(e => e.OutputData)
                .HasColumnType("text");

            entity.Property(e => e.ErrorMessage)
                .HasColumnType("text");

            entity.Property(e => e.StackTrace)
                .HasColumnType("text");

            entity.Property(e => e.CorrelationId)
                .HasMaxLength(100);

            entity.Property(e => e.TenantId)
                .HasMaxLength(100)
                .IsRequired();
        });

        // === WATCHES CONFIGURATION ===
        modelBuilder.Entity<Watch>(entity =>
        {
            entity.ToTable("Watches");

            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_Watch_TenantId");

            entity.HasIndex(e => new { e.TenantId, e.Status })
                .HasDatabaseName("IX_Watch_TenantId_Status");

            // Properties
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.EntityTypes)
                .HasColumnType("text");

            entity.Property(e => e.Condition)
                .HasColumnType("text")
                .IsRequired();

            entity.Property(e => e.NotificationConfig)
                .HasColumnType("text")
                .IsRequired();

            entity.Property(e => e.TenantId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100);

            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100);

            entity.Property(e => e.DeletedBy)
                .HasMaxLength(100);
        });

        modelBuilder.Entity<WatchEntity>(entity =>
        {
            entity.ToTable("WatchEntities");

            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => new { e.WatchId, e.TenantId })
                .HasDatabaseName("IX_WatchEntity_WatchId_TenantId");

            entity.HasIndex(e => e.EntityId)
                .HasDatabaseName("IX_WatchEntity_EntityId");

            // Properties
            entity.Property(e => e.EntityType)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.AddedBy)
                .HasMaxLength(100);

            entity.Property(e => e.StateSnapshot)
                .HasColumnType("text");

            entity.Property(e => e.TenantId)
                .HasMaxLength(100)
                .IsRequired();
        });

        modelBuilder.Entity<WatchTrigger>(entity =>
        {
            entity.ToTable("WatchTriggers");

            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => new { e.WatchId, e.TenantId })
                .HasDatabaseName("IX_WatchTrigger_WatchId_TenantId");

            entity.HasIndex(e => e.TriggeredAt)
                .HasDatabaseName("IX_WatchTrigger_TriggeredAt");

            // Properties
            entity.Property(e => e.EntityType)
                .HasMaxLength(200);

            entity.Property(e => e.ConditionMet)
                .HasColumnType("text");

            entity.Property(e => e.PreviousValue)
                .HasColumnType("text");

            entity.Property(e => e.CurrentValue)
                .HasColumnType("text");

            entity.Property(e => e.NotificationSent)
                .HasColumnType("text");

            entity.Property(e => e.ErrorMessage)
                .HasColumnType("text");

            entity.Property(e => e.AcknowledgedBy)
                .HasMaxLength(100);

            entity.Property(e => e.CorrelationId)
                .HasMaxLength(100);

            entity.Property(e => e.TenantId)
                .HasMaxLength(100)
                .IsRequired();
        });

        // === PATTERN TEMPLATES CONFIGURATION ===
        modelBuilder.Entity<PatternTemplate>(entity =>
        {
            entity.ToTable("PatternTemplates");

            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_PatternTemplate_TenantId");

            entity.HasIndex(e => new { e.TenantId, e.Category })
                .HasDatabaseName("IX_PatternTemplate_TenantId_Category");

            entity.HasIndex(e => e.IsPublic)
                .HasDatabaseName("IX_PatternTemplate_IsPublic")
                .HasFilter("[IsPublic] = 1");

            entity.HasIndex(e => e.IsOfficial)
                .HasDatabaseName("IX_PatternTemplate_IsOfficial")
                .HasFilter("[IsOfficial] = 1");

            entity.HasIndex(e => new { e.UsageCount, e.Rating })
                .HasDatabaseName("IX_PatternTemplate_UsageCount_Rating");

            // Properties
            entity.Property(e => e.TenantId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Content)
                .HasColumnType("text")
                .IsRequired();

            entity.Property(e => e.Tags)
                .HasColumnType("text")
                .IsRequired()
                .HasDefaultValue("[]");

            entity.Property(e => e.ThumbnailUrl)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100);

            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100);

            entity.Property(e => e.DeletedBy)
                .HasMaxLength(100);
        });

        modelBuilder.Entity<PatternTemplateRating>(entity =>
        {
            entity.ToTable("PatternTemplateRatings");

            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => new { e.TemplateId, e.UserId })
                .HasDatabaseName("IX_PatternTemplateRating_TemplateId_UserId")
                .IsUnique();

            entity.HasIndex(e => e.TemplateId)
                .HasDatabaseName("IX_PatternTemplateRating_TemplateId");

            // Properties
            entity.Property(e => e.TenantId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.UserId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Comment)
                .HasMaxLength(500);
        });
    }
}
