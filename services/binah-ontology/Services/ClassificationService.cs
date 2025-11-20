using Binah.Ontology.Models.Base;
using Binah.Ontology.Repositories;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Implementation of entity classification service
/// </summary>
public class ClassificationService : IClassificationService
{
    private readonly IDriver _neo4jDriver;
    private readonly IEntityRepository _entityRepository;
    private readonly ILogger<ClassificationService> _logger;

    public ClassificationService(
        IDriver neo4jDriver,
        IEntityRepository entityRepository,
        ILogger<ClassificationService> logger)
    {
        _neo4jDriver = neo4jDriver ?? throw new ArgumentNullException(nameof(neo4jDriver));
        _entityRepository = entityRepository ?? throw new ArgumentNullException(nameof(entityRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, object>> ClassifyEntityAsync(string tenantId, string entityId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(entityId))
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));

        _logger.LogInformation("Classifying entity {EntityId} in tenant {TenantId}", entityId, tenantId);

        var entity = await _entityRepository.GetByIdAsync(entityId);
        if (entity == null)
        {
            _logger.LogWarning("Entity {EntityId} not found", entityId);
            return new Dictionary<string, object>();
        }

        // Perform various classification operations
        var categories = await GetEntityCategoriesAsync(entity);
        var riskLevel = await DetermineRiskLevelAsync(entity);
        var qualityScore = await CalculateQualityScoreAsync(entity);
        var autoTags = await GenerateAutoTagsAsync(entity);

        // Update entity with classification results
        await UpdateEntityClassificationAsync(entity.Id, categories, riskLevel, qualityScore, autoTags);

        var results = new Dictionary<string, object>
        {
            { "categories", categories },
            { "risk_level", riskLevel },
            { "quality_score", qualityScore },
            { "auto_tags", autoTags },
            { "classified_at", DateTime.UtcNow }
        };

        _logger.LogInformation(
            "Entity {EntityId} classified: Categories={CategoryCount}, RiskLevel={RiskLevel}, QualityScore={QualityScore}",
            entityId, categories.Count, riskLevel, qualityScore);

        return results;
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetEntityCategoriesAsync(Entity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var categories = new List<string>();

        // Categorize based on entity type
        categories.Add(entity.Type);

        // Add categories based on properties
        if (entity.Properties.ContainsKey("price") || entity.Properties.ContainsKey("value"))
        {
            categories.Add("Financial");
        }

        if (entity.Properties.ContainsKey("latitude") && entity.Properties.ContainsKey("longitude"))
        {
            categories.Add("Geospatial");
        }

        if (entity.Properties.ContainsKey("address") || entity.Properties.ContainsKey("location"))
        {
            categories.Add("Physical");
        }

        // Check for temporal data
        if (entity.Properties.ContainsKey("start_date") || entity.Properties.ContainsKey("end_date"))
        {
            categories.Add("Temporal");
        }

        await Task.CompletedTask;
        return categories.Distinct().ToList();
    }

    /// <inheritdoc/>
    public async Task<string> DetermineRiskLevelAsync(Entity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var riskScore = 0;

        // Increase risk based on age
        var age = DateTime.UtcNow - entity.CreatedAt;
        if (age.TotalDays > 365)
            riskScore += 10;

        // Check for missing critical properties
        var criticalFields = new[] { "name", "type", "description" };
        var missingFields = criticalFields.Count(field => !entity.Properties.ContainsKey(field));
        riskScore += missingFields * 15;

        // Check for data quality issues
        if (entity.Metadata?.ContainsKey("validation_errors") == true)
            riskScore += 20;

        // Determine risk level based on score
        await Task.CompletedTask;

        return riskScore switch
        {
            < 20 => "low",
            < 40 => "medium",
            < 60 => "high",
            _ => "critical"
        };
    }

    /// <inheritdoc/>
    public async Task<int> CalculateQualityScoreAsync(Entity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var score = 100;

        // Penalize for missing properties
        if (entity.Properties.Count < 5)
            score -= 20;

        // Penalize for old data
        var age = DateTime.UtcNow - entity.UpdatedAt;
        if (age.TotalDays > 365)
            score -= 30;
        else if (age.TotalDays > 180)
            score -= 15;

        // Bonus for enrichment
        if (entity.Metadata?.ContainsKey("enriched_at") == true)
            score += 10;

        // Bonus for validation
        if (entity.Metadata?.ContainsKey("validated_at") == true)
            score += 10;

        // Bonus for relationships
        var relationshipCount = await GetRelationshipCountAsync(entity.Id, entity.TenantId ?? "core");
        if (relationshipCount > 5)
            score += 10;
        else if (relationshipCount > 0)
            score += 5;

        // Ensure score is within bounds
        score = Math.Max(0, Math.Min(100, score));

        return score;
    }

    /// <inheritdoc/>
    public async Task<List<string>> GenerateAutoTagsAsync(Entity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var tags = new List<string>();

        // Add entity type as tag
        tags.Add(entity.Type.ToLower());

        // Extract tags from properties
        foreach (var property in entity.Properties)
        {
            // Add tags based on property names
            if (property.Key.Contains("status", StringComparison.OrdinalIgnoreCase))
            {
                tags.Add($"status:{property.Value}");
            }

            if (property.Key.Contains("category", StringComparison.OrdinalIgnoreCase))
            {
                tags.Add($"category:{property.Value}");
            }
        }

        // Add temporal tags
        var age = DateTime.UtcNow - entity.CreatedAt;
        if (age.TotalDays < 30)
            tags.Add("new");
        else if (age.TotalDays > 365)
            tags.Add("old");

        // Add data quality tags
        var qualityScore = await CalculateQualityScoreAsync(entity);
        if (qualityScore >= 80)
            tags.Add("high-quality");
        else if (qualityScore < 50)
            tags.Add("needs-review");

        return tags.Distinct().ToList();
    }

    // Private helper methods

    private async Task<int> GetRelationshipCountAsync(string entityId, string tenantId)
    {
        await using var session = _neo4jDriver.AsyncSession();

        var query = @"
            MATCH (e:Entity {id: $entityId, tenantId: $tenantId})-[r]-()
            RETURN count(r) AS count
        ";

        try
        {
            var result = await session.RunAsync(query, new { entityId, tenantId });
            var records = await result.ToListAsync();
            return records.Count > 0 ? records[0]["count"].As<int>() : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get relationship count for entity {EntityId}", entityId);
            return 0;
        }
    }

    private async Task UpdateEntityClassificationAsync(
        string entityId,
        List<string> categories,
        string riskLevel,
        int qualityScore,
        List<string> autoTags)
    {
        await using var session = _neo4jDriver.AsyncSession();

        var query = @"
            MATCH (e:Entity {id: $entityId})
            SET e.categories = $categories,
                e.risk_level = $riskLevel,
                e.quality_score = $qualityScore,
                e.auto_tags = $autoTags,
                e.classified_at = datetime()
            RETURN e
        ";

        try
        {
            await session.RunAsync(query, new
            {
                entityId,
                categories,
                riskLevel,
                qualityScore,
                autoTags
            });

            _logger.LogDebug("Updated classification for entity {EntityId}", entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update classification for entity {EntityId}", entityId);
        }
    }
}
