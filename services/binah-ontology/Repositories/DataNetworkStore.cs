using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Binah.Ontology.Models.Base;
using Binah.Ontology.Pipelines.DataNetwork;
using Binah.Ontology.Infrastructure;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Binah.Ontology.Repositories
{
    /// <summary>
    /// Neo4j repository for storing scrubbed entities in the data network
    /// Uses separate Neo4j instance for complete isolation from production data
    /// </summary>
    public class DataNetworkStore : IDataNetworkStore
    {
        private readonly IDriver _driver;
        private readonly ILogger<DataNetworkStore> _logger;

        // Separate Neo4j database for data network
        private const string DataNetworkDatabase = "data-network";

        public DataNetworkStore(
            IDataNetworkNeo4jDriver dataNetworkDriver,
            ILogger<DataNetworkStore> logger)
        {
            if (dataNetworkDriver == null)
                throw new ArgumentNullException(nameof(dataNetworkDriver));

            _driver = dataNetworkDriver.Driver;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task StoreAsync(Entity entity, DataNetworkMetadata metadata)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            // Use separate database for data network
            var session = _driver.AsyncSession(config => config.WithDatabase(DataNetworkDatabase));
            try
            {
                await session.ExecuteWriteAsync(async tx =>
                {
                    // Create node with DataNetwork label plus entity type label
                    var labels = $"DataNetwork:{metadata.EntityType}";

                    var query = $@"
                        CREATE (e:{labels})
                        SET e = $properties
                        SET e.domain = $domain
                        SET e.entity_type = $entity_type
                        SET e.scrubbed = true
                        SET e.scrubbing_level = $scrubbing_level
                        SET e.original_tenant_hash = $tenant_hash
                        SET e.consent_version = $consent_version
                        SET e.ingested_at = datetime($ingested_at)
                        RETURN e
                    ";

                    // Flatten entity properties for Neo4j storage
                    var flatProperties = new Dictionary<string, object>();

                    // Add Entity base properties
                    if (!string.IsNullOrEmpty(entity.Id))
                        flatProperties["id"] = entity.Id;

                    if (!string.IsNullOrEmpty(entity.Type))
                        flatProperties["type"] = entity.Type;

                    // Add all custom properties from the dynamic property bag
                    foreach (var prop in entity.Properties)
                    {
                        // Convert complex objects to JSON strings for Neo4j compatibility
                        if (prop.Value != null)
                        {
                            if (IsSimpleType(prop.Value.GetType()))
                            {
                                flatProperties[prop.Key] = prop.Value;
                            }
                            else
                            {
                                // Serialize complex types to JSON
                                flatProperties[prop.Key] = JsonSerializer.Serialize(prop.Value);
                            }
                        }
                    }

                    var parameters = new
                    {
                        properties = flatProperties,
                        domain = metadata.Domain,
                        entity_type = metadata.EntityType,
                        scrubbing_level = metadata.ScrubbingLevel.ToString(),
                        tenant_hash = metadata.OriginalTenantHash,
                        consent_version = metadata.ConsentVersion,
                        ingested_at = metadata.IngestedAt.ToString("O") // ISO 8601 format
                    };

                    await tx.RunAsync(query, parameters);
                });

                _logger.LogInformation(
                    "Stored {EntityType} in data network (domain: {Domain}, scrubbing: {ScrubbingLevel})",
                    metadata.EntityType,
                    metadata.Domain,
                    metadata.ScrubbingLevel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to store entity {EntityType} in data network for domain {Domain}",
                    metadata.EntityType,
                    metadata.Domain);
                throw;
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        /// <summary>
        /// Check if a type is a simple type that Neo4j can store directly
        /// </summary>
        private static bool IsSimpleType(Type type)
        {
            return type.IsPrimitive
                   || type == typeof(string)
                   || type == typeof(decimal)
                   || type == typeof(DateTime)
                   || type == typeof(DateTimeOffset)
                   || type == typeof(Guid)
                   || type.IsEnum;
        }
    }
}
