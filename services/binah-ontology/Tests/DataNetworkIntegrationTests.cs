using FluentAssertions;
using Xunit;
using Neo4j.Driver;
using Testcontainers.Neo4j;
using Microsoft.Extensions.Logging;
using Moq;
using Binah.Ontology.Models.Base;
using Binah.Ontology.Models.Tenant;
using Binah.Ontology.Pipelines.DataNetwork;
using Binah.Ontology.Repositories;
using Binah.Ontology.Services;
// TODO: Uncomment once Finance domain is generated via binah-regen service
// using Finance.Domain.Pipelines.DataNetwork;

namespace Binah.Ontology.Tests;

/// <summary>
/// Integration tests for data network PII scrubbing and contribution
/// Tests the full pipeline: consent validation → PII scrubbing → data network storage
/// </summary>
public class DataNetworkIntegrationTests : IAsyncLifetime
{
    private Neo4jContainer? _productionNeo4j;
    private Neo4jContainer? _dataNetworkNeo4j;
    private IDriver? _productionDriver;
    private IDriver? _dataNetworkDriver;
    private Mock<ITenantRepository>? _tenantRepoMock;
    private Mock<ILogger<DataNetworkPipeline>>? _loggerMock;

    public async Task InitializeAsync()
    {
        // Start two Neo4j containers to simulate production and data network isolation
        _productionNeo4j = new Neo4jBuilder()
            .WithPassword("prodpassword")
            .Build();

        _dataNetworkNeo4j = new Neo4jBuilder()
            .WithPassword("datanetworkpassword")
            .Build();

        await Task.WhenAll(
            _productionNeo4j.StartAsync(),
            _dataNetworkNeo4j.StartAsync()
        );

        _productionDriver = GraphDatabase.Driver(
            _productionNeo4j.GetConnectionString(),
            AuthTokens.Basic("neo4j", "prodpassword"));

        _dataNetworkDriver = GraphDatabase.Driver(
            _dataNetworkNeo4j.GetConnectionString(),
            AuthTokens.Basic("neo4j", "datanetworkpassword"));

        _tenantRepoMock = new Mock<ITenantRepository>();
        _loggerMock = new Mock<ILogger<DataNetworkPipeline>>();
    }

    [Fact]
    public async Task ProcessEntity_WithConsent_ContributesToDataNetwork()
    {
        // Arrange
        var tenantId = "tenant_consent_yes";
        var tenant = CreateTenantWithConsent(tenantId, hasConsent: true, ScrubbingLevel.Strict);
        _tenantRepoMock!.Setup(r => r.GetByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        var consentValidator = new TenantConsentValidator(_tenantRepoMock.Object);
        var piiScrubber = new PiiScrubber(ScrubbingLevel.Strict, tokenService: null);
        var dataNetworkStore = new DataNetworkStore(_dataNetworkDriver!);
        var pipeline = new DataNetworkPipeline(consentValidator, piiScrubber, dataNetworkStore, _loggerMock!.Object);

        var entity = CreateTestClient(tenantId, "John Doe", "john.doe@example.com", "555-1234");

        // Act
        var result = await pipeline.ProcessEntityAsync(entity);

        // Assert
        result.Should().BeTrue("entity should be contributed when tenant has consent");

        // Verify entity exists in data network Neo4j
        var dataNetworkSession = _dataNetworkDriver!.AsyncSession();
        try
        {
            var count = await dataNetworkSession.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(
                    "MATCH (e:Entity) WHERE e.metadata_scrubbed = true RETURN count(e) as count");
                var record = await cursor.SingleAsync();
                return record["count"].As<int>();
            });

            count.Should().BeGreaterThan(0, "scrubbed entity should exist in data network");
        }
        finally
        {
            await dataNetworkSession.CloseAsync();
        }
    }

    [Fact]
    public async Task ProcessEntity_WithoutConsent_DoesNotContribute()
    {
        // Arrange
        var tenantId = "tenant_consent_no";
        var tenant = CreateTenantWithConsent(tenantId, hasConsent: false, ScrubbingLevel.Strict);
        _tenantRepoMock!.Setup(r => r.GetByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        var consentValidator = new TenantConsentValidator(_tenantRepoMock.Object);
        var piiScrubber = new PiiScrubber(ScrubbingLevel.Strict, tokenService: null);
        var dataNetworkStore = new DataNetworkStore(_dataNetworkDriver!);
        var pipeline = new DataNetworkPipeline(consentValidator, piiScrubber, dataNetworkStore, _loggerMock!.Object);

        var entity = CreateTestClient(tenantId, "Jane Doe", "jane.doe@example.com", "555-5678");

        // Act
        var result = await pipeline.ProcessEntityAsync(entity);

        // Assert
        result.Should().BeFalse("entity should not be contributed when tenant lacks consent");
    }

    [Fact]
    public async Task ProcessEntity_WithCategoryFilter_OnlyContributesSelectedTypes()
    {
        // Arrange
        var tenantId = "tenant_category_filter";
        var tenant = CreateTenantWithConsent(tenantId, hasConsent: true, ScrubbingLevel.Strict);
        tenant.DataNetworkCategories = new List<string> { "Client", "Account" }; // Only these types
        _tenantRepoMock!.Setup(r => r.GetByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        var consentValidator = new TenantConsentValidator(_tenantRepoMock.Object);
        var piiScrubber = new PiiScrubber(ScrubbingLevel.Strict, tokenService: null);
        var dataNetworkStore = new DataNetworkStore(_dataNetworkDriver!);
        var pipeline = new DataNetworkPipeline(consentValidator, piiScrubber, dataNetworkStore, _loggerMock!.Object);

        var clientEntity = CreateTestClient(tenantId, "Allowed Client", "allowed@example.com", "555-1111");
        var transactionEntity = CreateTestTransaction(tenantId, 1000.00m);

        // Act
        var clientResult = await pipeline.ProcessEntityAsync(clientEntity);
        var transactionResult = await pipeline.ProcessEntityAsync(transactionEntity);

        // Assert
        clientResult.Should().BeTrue("Client is in allowed categories");
        transactionResult.Should().BeFalse("Transaction is not in allowed categories");
    }

    [Fact]
    public async Task PiiScrubber_StrictLevel_RemovesAllPii()
    {
        // Arrange
        var scrubber = new PiiScrubber(ScrubbingLevel.Strict, tokenService: null);
        var entity = CreateTestClient("tenant_1", "John Smith", "john.smith@example.com", "555-9999");
        entity.SetPropertyValue("ssn", "123-45-6789");
        entity.SetPropertyValue("dateOfBirth", new DateTime(1980, 5, 15));

        // Act
        var scrubbed = scrubber.ScrubEntity(entity, "Client");

        // Assert
        scrubbed.GetPropertyValue<string>("name").Should().BeNull("name should be removed in Strict mode");
        scrubbed.GetPropertyValue<string>("email").Should().BeNull("email should be removed in Strict mode");
        scrubbed.GetPropertyValue<string>("phone").Should().BeNull("phone should be removed in Strict mode");
        scrubbed.GetPropertyValue<string>("ssn").Should().BeNull("SSN should be removed in Strict mode");
        scrubbed.TenantId.Should().BeNull("tenant ID should be removed");
        scrubbed.Metadata.Should().ContainKey("scrubbed").WhoseValue.Should().Be(true);
        scrubbed.Metadata.Should().ContainKey("original_tenant_id_hash");

        // Date should be generalized to month
        var dob = scrubbed.GetPropertyValue<DateTime?>("dateOfBirth");
        if (dob.HasValue)
        {
            dob.Value.Day.Should().Be(1, "day should be generalized to 1st of month");
        }
    }

    [Fact]
    public async Task PiiScrubber_ModerateLevel_TokenizesPii()
    {
        // Arrange
        var scrubber = new PiiScrubber(ScrubbingLevel.Moderate, tokenService: null);
        var entity = CreateTestClient("tenant_1", "Jane Doe", "jane@example.com", "555-7777");
        var originalEmail = entity.GetPropertyValue<string>("email");

        // Act
        var scrubbed = scrubber.ScrubEntity(entity, "Client");

        // Assert
        scrubbed.GetPropertyValue<string>("name").Should().NotBe("Jane Doe", "name should be tokenized");
        scrubbed.GetPropertyValue<string>("email").Should().NotBe(originalEmail, "email should be tokenized");
        scrubbed.Metadata.Should().ContainKey("scrubbed").WhoseValue.Should().Be(true);
    }

    [Fact]
    public async Task PiiScrubber_MinimalLevel_OnlyRemovesSensitiveFields()
    {
        // Arrange
        var scrubber = new PiiScrubber(ScrubbingLevel.Minimal, tokenService: null);
        var entity = CreateTestClient("tenant_1", "Bob Johnson", "bob@example.com", "555-8888");
        entity.SetPropertyValue("ssn", "999-88-7777");
        entity.SetPropertyValue("accountBalance", 50000.00m);

        // Act
        var scrubbed = scrubber.ScrubEntity(entity, "Client");

        // Assert
        scrubbed.GetPropertyValue<string>("ssn").Should().BeNull("SSN should be removed even in Minimal mode");
        scrubbed.GetPropertyValue<string>("name").Should().Be("Bob Johnson", "name should be kept in Minimal mode");
        scrubbed.GetPropertyValue<string>("email").Should().Be("bob@example.com", "email should be kept in Minimal mode");
        scrubbed.GetPropertyValue<decimal>("accountBalance").Should().Be(50000.00m, "non-PII financial data should be kept");
    }

    [Fact]
    public async Task DataNetworkStore_IsolatesDataFromProduction()
    {
        // Arrange
        var dataNetworkStore = new DataNetworkStore(_dataNetworkDriver!);
        var entity = CreateTestClient("tenant_isolated", "Test User", "test@example.com", "555-0000");
        var scrubber = new PiiScrubber(ScrubbingLevel.Strict, tokenService: null);
        var scrubbed = scrubber.ScrubEntity(entity, "Client");

        var metadata = new DataNetworkMetadata
        {
            Domain = "Finance",
            EntityType = "Client",
            OriginalTenantHash = "hash_tenant_isolated",
            ScrubbingLevel = ScrubbingLevel.Strict,
            ConsentVersion = "1.0",
            IngestedAt = DateTime.UtcNow
        };

        // Act
        await dataNetworkStore.StoreAsync(scrubbed, metadata);

        // Assert - Verify entity in data network
        var dataNetworkSession = _dataNetworkDriver!.AsyncSession();
        try
        {
            var foundInDataNetwork = await dataNetworkSession.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(
                    "MATCH (e:Entity {id: $id}) RETURN count(e) as count",
                    new { id = scrubbed.Id });
                var record = await cursor.SingleAsync();
                return record["count"].As<int>() > 0;
            });

            foundInDataNetwork.Should().BeTrue("entity should be in data network Neo4j");
        }
        finally
        {
            await dataNetworkSession.CloseAsync();
        }

        // Assert - Verify entity NOT in production
        var productionSession = _productionDriver!.AsyncSession();
        try
        {
            var foundInProduction = await productionSession.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(
                    "MATCH (e:Entity {id: $id}) RETURN count(e) as count",
                    new { id = scrubbed.Id });
                var record = await cursor.SingleAsync();
                return record["count"].As<int>() > 0;
            });

            foundInProduction.Should().BeFalse("entity should NOT be in production Neo4j");
        }
        finally
        {
            await productionSession.CloseAsync();
        }
    }

    [Fact]
    public async Task PiiScrubber_HashesEntityAndTenantIds()
    {
        // Arrange
        var scrubber = new PiiScrubber(ScrubbingLevel.Strict, tokenService: null);
        var tenantId = "tenant_hash_test";
        var entityId = "client_12345";
        var entity = CreateTestClient(tenantId, "Hash Test", "hash@example.com", "555-0001");
        entity.Id = entityId;

        // Act
        var scrubbed = scrubber.ScrubEntity(entity, "Client");

        // Assert
        scrubbed.Id.Should().NotBe(entityId, "entity ID should be hashed");
        scrubbed.TenantId.Should().BeNull("tenant ID should be removed");
        scrubbed.Metadata.Should().ContainKey("original_tenant_id_hash");

        var tenantHash = scrubbed.Metadata["original_tenant_id_hash"] as string;
        tenantHash.Should().NotBeNullOrEmpty("should have tenant ID hash in metadata");
        tenantHash.Should().NotBe(tenantId, "should be hashed, not original ID");
    }

    [Fact]
    public async Task ConsentValidator_RespectsConsentVersion()
    {
        // Arrange
        var tenantId = "tenant_version_test";
        var tenant = CreateTenantWithConsent(tenantId, hasConsent: true, ScrubbingLevel.Strict);
        tenant.DataNetworkConsentVersion = "1.0";
        _tenantRepoMock!.Setup(r => r.GetByIdAsync(tenantId))
            .ReturnsAsync(tenant);

        var consentValidator = new TenantConsentValidator(_tenantRepoMock.Object);

        // Act
        var consent = await consentValidator.ValidateConsentAsync(tenantId, "Client");

        // Assert
        consent.HasConsent.Should().BeTrue();
        consent.ConsentVersion.Should().Be("1.0");
        consent.ScrubbingLevel.Should().Be(ScrubbingLevel.Strict);
    }

    // === Helper Methods ===

    private Tenant CreateTenantWithConsent(string tenantId, bool hasConsent, ScrubbingLevel level)
    {
        return new Tenant
        {
            Id = tenantId,
            Name = $"Tenant {tenantId}",
            IsActive = true,
            DataNetworkConsent = hasConsent,
            DataNetworkConsentDate = hasConsent ? DateTime.UtcNow : null,
            DataNetworkConsentVersion = "1.0",
            PiiScrubbingLevel = level,
            DataNetworkCategories = new List<string>() // Empty = all types
        };
    }

    private Entity CreateTestClient(string tenantId, string name, string email, string phone)
    {
        var entity = new Entity
        {
            Id = $"client_{Guid.NewGuid():N}",
            Type = "Client",
            TenantId = tenantId,
            Properties = new Dictionary<string, object>()
        };

        entity.SetPropertyValue("name", name);
        entity.SetPropertyValue("email", email);
        entity.SetPropertyValue("phone", phone);
        entity.SetPropertyValue("status", "Active");

        return entity;
    }

    private Entity CreateTestTransaction(string tenantId, decimal amount)
    {
        var entity = new Entity
        {
            Id = $"transaction_{Guid.NewGuid():N}",
            Type = "Transaction",
            TenantId = tenantId,
            Properties = new Dictionary<string, object>()
        };

        entity.SetPropertyValue("amount", amount);
        entity.SetPropertyValue("date", DateTime.UtcNow);
        entity.SetPropertyValue("status", "Completed");

        return entity;
    }

    public async Task DisposeAsync()
    {
        if (_productionDriver != null)
            await _productionDriver.DisposeAsync();

        if (_dataNetworkDriver != null)
            await _dataNetworkDriver.DisposeAsync();

        if (_productionNeo4j != null)
            await _productionNeo4j.DisposeAsync();

        if (_dataNetworkNeo4j != null)
            await _dataNetworkNeo4j.DisposeAsync();
    }
}
