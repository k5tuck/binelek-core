using Binah.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Binah.Auth.Models;

/// <summary>
/// Database context for authentication
/// </summary>
public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<SamlConfiguration> SamlConfigurations { get; set; } = null!;
    public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; } = null!;
    public DbSet<WebhookDelivery> WebhookDeliveries { get; set; } = null!;
    public DbSet<Client> Clients { get; set; } = null!;

    // Team Workspace
    public DbSet<TeamMember> TeamMembers { get; set; } = null!;
    public DbSet<TeamInvitation> TeamInvitations { get; set; } = null!;
    public DbSet<SharedResource> SharedResources { get; set; } = null!;
    public DbSet<TeamActivity> TeamActivities { get; set; } = null!;

    // API Keys
    public DbSet<ApiKey> ApiKeys { get; set; } = null!;
    public DbSet<ApiKeyUsage> ApiKeyUsages { get; set; } = null!;

    // SSO Configuration
    public DbSet<SsoConfig> SsoConfigs { get; set; } = null!;
    public DbSet<DomainVerification> DomainVerifications { get; set; } = null!;

    // User Favorites
    public DbSet<UserFavorite> UserFavorites { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Roles).HasColumnType("jsonb");
        });

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.Token).IsRequired();
            entity.HasOne<User>()
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Company configuration
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.HasMany(c => c.Users)
                .WithOne()
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("audit_logs");
            entity.HasIndex(e => new { e.TenantId, e.Timestamp })
                .HasDatabaseName("idx_audit_tenant_timestamp");
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("idx_audit_user");
            entity.HasIndex(e => new { e.Resource, e.ResourceId })
                .HasDatabaseName("idx_audit_resource");
            entity.HasIndex(e => e.Action)
                .HasDatabaseName("idx_audit_action");

            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Resource).HasMaxLength(255);
            entity.Property(e => e.ResourceId).HasMaxLength(255);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.RequestPath).HasMaxLength(500);
            entity.Property(e => e.HttpMethod).HasMaxLength(10);
            entity.Property(e => e.Details).HasColumnType("jsonb");
            entity.Property(e => e.Timestamp).IsRequired().HasDefaultValueSql("NOW()");
        });

        // SamlConfiguration configuration
        modelBuilder.Entity<SamlConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("saml_configurations");
            entity.HasIndex(e => e.TenantId).IsUnique();

            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(500);
            entity.Property(e => e.SsoUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.X509Certificate).IsRequired();
            entity.Property(e => e.AttributeMapping).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");
        });

        // WebhookSubscription configuration
        modelBuilder.Entity<WebhookSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("webhook_subscriptions");
            entity.HasIndex(e => e.TenantId);

            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Events).HasColumnType("jsonb");
            entity.Property(e => e.Secret).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Headers).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");
        });

        // WebhookDelivery configuration
        modelBuilder.Entity<WebhookDelivery>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("webhook_deliveries");
            entity.HasIndex(e => e.SubscriptionId);

            entity.Property(e => e.EventType).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Payload).HasColumnType("jsonb");
            entity.Property(e => e.ResponseStatus).HasMaxLength(50);
            entity.Property(e => e.DeliveredAt).IsRequired().HasDefaultValueSql("NOW()");
        });

        // Client configuration
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("clients");

            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_clients_TenantId");
            entity.HasIndex(e => e.Email).HasDatabaseName("IX_clients_Email");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_clients_Status");
            entity.HasIndex(e => new { e.TenantId, e.Status }).HasDatabaseName("IX_clients_TenantId_Status");
            entity.HasIndex(e => new { e.TenantId, e.CreatedAt }).HasDatabaseName("IX_clients_TenantId_CreatedAt");

            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Company).HasMaxLength(255);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("active");
            entity.Property(e => e.Tags).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
            entity.Property(e => e.CustomFields).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");

            // Soft delete filter
            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        // TeamMember configuration
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("team_members");

            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_team_members_TenantId");
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_team_members_UserId");
            entity.HasIndex(e => new { e.TenantId, e.UserId }).IsUnique().HasDatabaseName("IX_team_members_TenantId_UserId");

            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DisplayName).HasMaxLength(255);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Permissions).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");
        });

        // TeamInvitation configuration
        modelBuilder.Entity<TeamInvitation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("team_invitations");

            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_team_invitations_TenantId");
            entity.HasIndex(e => e.Token).IsUnique().HasDatabaseName("IX_team_invitations_Token");
            entity.HasIndex(e => e.Email).HasDatabaseName("IX_team_invitations_Email");

            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(255);
            entity.Property(e => e.InvitedBy).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.SentAt).IsRequired().HasDefaultValueSql("NOW()");
            entity.Property(e => e.ExpiresAt).IsRequired();
        });

        // SharedResource configuration
        modelBuilder.Entity<SharedResource>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("shared_resources");

            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_shared_resources_TenantId");
            entity.HasIndex(e => new { e.ResourceType, e.ResourceId }).HasDatabaseName("IX_shared_resources_ResourceType_ResourceId");

            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ResourceType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ResourceId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.SharedBy).IsRequired().HasMaxLength(255);
            entity.Property(e => e.AccessLevel).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SharedWithUsers).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
            entity.Property(e => e.SharedWithRoles).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");
        });

        // TeamActivity configuration
        modelBuilder.Entity<TeamActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("team_activities");

            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_team_activities_TenantId");
            entity.HasIndex(e => new { e.TenantId, e.Timestamp }).HasDatabaseName("IX_team_activities_TenantId_Timestamp");

            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UserDisplayName).HasMaxLength(255);
            entity.Property(e => e.ActivityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ResourceType).HasMaxLength(100);
            entity.Property(e => e.ResourceId).HasMaxLength(255);
            entity.Property(e => e.ResourceName).HasMaxLength(255);
            entity.Property(e => e.Metadata).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            entity.Property(e => e.Timestamp).IsRequired().HasDefaultValueSql("NOW()");
        });

        // ApiKey configuration
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("api_keys");

            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_api_keys_TenantId");
            entity.HasIndex(e => e.KeyHash).IsUnique().HasDatabaseName("IX_api_keys_KeyHash");
            entity.HasIndex(e => e.UserId).HasDatabaseName("IX_api_keys_UserId");

            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.KeyPrefix).IsRequired().HasMaxLength(50);
            entity.Property(e => e.KeyHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Scopes).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
            entity.Property(e => e.Environment).IsRequired().HasMaxLength(20);
            entity.Property(e => e.AllowedIps).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
            entity.Property(e => e.Metadata).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
            entity.Property(e => e.LastUsedIp).HasMaxLength(45);
            entity.Property(e => e.RevokedBy).HasMaxLength(255);
            entity.Property(e => e.RevocationReason).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");
        });

        // ApiKeyUsage configuration
        modelBuilder.Entity<ApiKeyUsage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("api_key_usages");

            entity.HasIndex(e => e.ApiKeyId).HasDatabaseName("IX_api_key_usages_ApiKeyId");
            entity.HasIndex(e => new { e.ApiKeyId, e.Timestamp }).HasDatabaseName("IX_api_key_usages_ApiKeyId_Timestamp");

            entity.Property(e => e.ApiKeyId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.HttpMethod).IsRequired().HasMaxLength(10);
            entity.Property(e => e.RequestPath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Timestamp).IsRequired().HasDefaultValueSql("NOW()");
        });

        // SsoConfig configuration
        modelBuilder.Entity<SsoConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("sso_configs");

            entity.HasIndex(e => e.TenantId).IsUnique().HasDatabaseName("IX_sso_configs_TenantId");

            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ProviderType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);

            // SAML
            entity.Property(e => e.SamlEntityId).HasMaxLength(500);
            entity.Property(e => e.SamlSsoUrl).HasMaxLength(500);
            entity.Property(e => e.SamlSloUrl).HasMaxLength(500);
            entity.Property(e => e.SamlAttributeMapping).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");

            // OIDC
            entity.Property(e => e.OidcClientId).HasMaxLength(255);
            entity.Property(e => e.OidcIssuer).HasMaxLength(500);
            entity.Property(e => e.OidcAuthorizationEndpoint).HasMaxLength(500);
            entity.Property(e => e.OidcTokenEndpoint).HasMaxLength(500);
            entity.Property(e => e.OidcUserInfoEndpoint).HasMaxLength(500);
            entity.Property(e => e.OidcScopes).HasColumnType("jsonb").HasDefaultValueSql("'[\"openid\", \"email\", \"profile\"]'::jsonb");
            entity.Property(e => e.OidcClaimMapping).HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");

            // Domain and settings
            entity.Property(e => e.VerifiedDomains).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
            entity.Property(e => e.DefaultRole).IsRequired().HasMaxLength(50).HasDefaultValue("User");
            entity.Property(e => e.LastTestResult).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");
        });

        // DomainVerification configuration
        modelBuilder.Entity<DomainVerification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("domain_verifications");

            entity.HasIndex(e => e.TenantId).HasDatabaseName("IX_domain_verifications_TenantId");
            entity.HasIndex(e => e.Domain).IsUnique().HasDatabaseName("IX_domain_verifications_Domain");

            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.SsoConfigId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Domain).IsRequired().HasMaxLength(255);
            entity.Property(e => e.VerificationMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.VerificationToken).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
        });

        // UserFavorite configuration
        modelBuilder.Entity<UserFavorite>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("user_favorites");

            entity.HasIndex(e => new { e.UserId, e.TenantId }).HasDatabaseName("IX_user_favorites_UserId_TenantId");
            entity.HasIndex(e => new { e.UserId, e.TenantId, e.Route }).IsUnique().HasDatabaseName("IX_user_favorites_UserId_TenantId_Route");

            entity.Property(e => e.UserId).IsRequired().HasMaxLength(36);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(36);
            entity.Property(e => e.Route).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Label).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
        });
    }
}
