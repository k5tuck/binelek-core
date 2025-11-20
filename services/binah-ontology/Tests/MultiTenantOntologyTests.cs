using FluentAssertions;
using Xunit;
using Neo4j.Driver;
using Testcontainers.Neo4j;

namespace Binah.Ontology.Tests;

/// <summary>
/// Tests for multi-tenant ontology functionality
/// Verifies that tenant-specific extensions work correctly with core ontology
/// </summary>
public class MultiTenantOntologyTests : IAsyncLifetime
{
    private Neo4jContainer? _neo4jContainer;
    private IDriver? _driver;

    public async Task InitializeAsync()
    {
        _neo4jContainer = new Neo4jBuilder()
            .WithPassword("testpassword")
            .Build();

        await _neo4jContainer.StartAsync();

        _driver = GraphDatabase.Driver(
            _neo4jContainer.GetConnectionString(),
            AuthTokens.Basic("neo4j", "testpassword"));
    }

    [Fact]
    public async Task CreateCoreEntity_WithCoreLabel_Succeeds()
    {
        // Arrange
        var session = _driver!.AsyncSession();

        try
        {
            // Act - Create core Property entity
            var result = await session.ExecuteWriteAsync(async tx =>
            {
                var query = @"
                    CREATE (p:Property:Core {
                        id: $id,
                        tenantId: 'core',
                        street: $street,
                        city: $city,
                        state: $state,
                        squareFeet: $squareFeet
                    })
                    RETURN p.id as id";

                var cursor = await tx.RunAsync(query, new
                {
                    id = "prop_test_123",
                    street = "123 Test St",
                    city = "Austin",
                    state = "TX",
                    squareFeet = 2500
                });

                var record = await cursor.SingleAsync();
                return record["id"].As<string>();
            });

            // Assert
            result.Should().Be("prop_test_123");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [Fact]
    public async Task CreateTenantExtension_WithExtendsRelationship_Succeeds()
    {
        // Arrange
        var session = _driver!.AsyncSession();

        try
        {
            // Create core entity first
            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(@"
                    CREATE (p:Property:Core {
                        id: 'prop_core_456',
                        tenantId: 'core',
                        street: '456 Main St',
                        city: 'Dallas',
                        state: 'TX'
                    })");
            });

            // Act - Create tenant-specific extension
            var result = await session.ExecuteWriteAsync(async tx =>
            {
                var query = @"
                    MATCH (core:Property:Core {id: $coreId})
                    CREATE (ext:PropertyExtension {
                        id: $extId,
                        tenantId: $tenantId,
                        energyScore: $energyScore,
                        solarPanels: $solarPanels,
                        estimatedSavings: $estimatedSavings
                    })
                    CREATE (ext)-[:EXTENDS]->(core)
                    RETURN ext.id as id, ext.tenantId as tenantId";

                var cursor = await tx.RunAsync(query, new
                {
                    coreId = "prop_core_456",
                    extId = "prop_ext_tenant_a_456",
                    tenantId = "tenant_a",
                    energyScore = 95,
                    solarPanels = true,
                    estimatedSavings = 15000
                });

                var record = await cursor.SingleAsync();
                return new
                {
                    Id = record["id"].As<string>(),
                    TenantId = record["tenantId"].As<string>()
                };
            });

            // Assert
            result.TenantId.Should().Be("tenant_a");
            result.Id.Should().Contain("tenant_a");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [Fact]
    public async Task QueryTenantData_OnlyReturnsTenantEntities()
    {
        // Arrange
        var session = _driver!.AsyncSession();
        var tenantA = "tenant_a";
        var tenantB = "tenant_b";

        try
        {
            // Create core entity and extensions for two tenants
            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(@"
                    CREATE (core:Property:Core {id: 'prop_789', tenantId: 'core', street: '789 Oak St'})
                    CREATE (extA:PropertyExtension {id: 'ext_a_789', tenantId: 'tenant_a', customField: 'A data'})
                    CREATE (extB:PropertyExtension {id: 'ext_b_789', tenantId: 'tenant_b', customField: 'B data'})
                    CREATE (extA)-[:EXTENDS]->(core)
                    CREATE (extB)-[:EXTENDS]->(core)");
            });

            // Act - Query for tenant A's data only
            var tenantAData = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (ext:PropertyExtension {tenantId: $tenantId})-[:EXTENDS]->(core:Property:Core)
                    RETURN ext.id as extId, ext.customField as customField, core.street as street";

                var cursor = await tx.RunAsync(query, new { tenantId = tenantA });
                var records = await cursor.ToListAsync();

                return records.Select(r => new
                {
                    ExtId = r["extId"].As<string>(),
                    CustomField = r["customField"].As<string>(),
                    Street = r["street"].As<string>()
                }).ToList();
            });

            // Assert
            tenantAData.Should().HaveCount(1);
            tenantAData[0].CustomField.Should().Be("A data");
            tenantAData[0].ExtId.Should().Contain("tenant_a");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [Fact]
    public async Task TenantIsolation_PreventsCrosstenantQueries()
    {
        // Arrange
        var session = _driver!.AsyncSession();

        try
        {
            // Create data for two tenants
            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(@"
                    CREATE (pA:Property {id: 'prop_tenant_a_1', tenantId: 'tenant_a', owner: 'Owner A'})
                    CREATE (pB:Property {id: 'prop_tenant_b_1', tenantId: 'tenant_b', owner: 'Owner B'})");
            });

            // Act - Query with tenant isolation
            var tenantAOnly = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (p:Property {tenantId: $tenantId})
                    RETURN count(p) as count";

                var cursor = await tx.RunAsync(query, new { tenantId = "tenant_a" });
                var record = await cursor.SingleAsync();
                return record["count"].As<int>();
            });

            // Assert
            tenantAOnly.Should().Be(1, "should only return tenant A's property");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [Fact]
    public async Task CoreOntologyQuery_ReturnsAllCoreEntities()
    {
        // Arrange
        var session = _driver!.AsyncSession();

        try
        {
            // Create multiple core entities
            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(@"
                    CREATE (p1:Property:Core {id: 'core_1', tenantId: 'core'})
                    CREATE (p2:Property:Core {id: 'core_2', tenantId: 'core'})
                    CREATE (p3:Property:Core {id: 'core_3', tenantId: 'core'})");
            });

            // Act - Query all core properties
            var coreCount = await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync("MATCH (p:Core) RETURN count(p) as count");
                var record = await cursor.SingleAsync();
                return record["count"].As<int>();
            });

            // Assert
            coreCount.Should().BeGreaterThanOrEqualTo(3);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    [Fact]
    public async Task ExtensionInheritance_CanAccessCoreProperties()
    {
        // Arrange
        var session = _driver!.AsyncSession();

        try
        {
            // Setup: Create core with extension
            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(@"
                    CREATE (core:Property:Core {
                        id: 'prop_inherit_test',
                        tenantId: 'core',
                        street: 'Inheritance St',
                        listPrice: 500000
                    })
                    CREATE (ext:PropertyExtension {
                        id: 'ext_inherit_test',
                        tenantId: 'tenant_x',
                        customMetric: 42
                    })
                    CREATE (ext)-[:EXTENDS]->(core)");
            });

            // Act - Query extension and access core properties
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var query = @"
                    MATCH (ext:PropertyExtension {id: $extId})-[:EXTENDS]->(core:Property:Core)
                    RETURN ext.customMetric as customMetric, 
                           core.street as street,
                           core.listPrice as listPrice";

                var cursor = await tx.RunAsync(query, new { extId = "ext_inherit_test" });
                var record = await cursor.SingleAsync();

                return new
                {
                    CustomMetric = record["customMetric"].As<int>(),
                    Street = record["street"].As<string>(),
                    ListPrice = record["listPrice"].As<int>()
                };
            });

            // Assert
            result.CustomMetric.Should().Be(42, "extension property should be accessible");
            result.Street.Should().Be("Inheritance St", "core property should be accessible via EXTENDS");
            result.ListPrice.Should().Be(500000, "core property should be accessible");
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (_driver != null)
        {
            await _driver.DisposeAsync();
        }

        if (_neo4jContainer != null)
        {
            await _neo4jContainer.DisposeAsync();
        }
    }
}
