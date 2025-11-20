using Binah.Ontology.Models;
using Binah.Ontology.Models.Exceptions;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Implementation of query service for executing Cypher queries and graph traversals
/// </summary>
public class QueryService : IQueryService
{
    private readonly IDriver _driver;
    private readonly string _database;
    private readonly ILogger<QueryService> _logger;

    // Forbidden operations for security
    private static readonly string[] ForbiddenOperations = new[]
    {
        "DELETE",
        "DETACH DELETE",
        "REMOVE",
        "DROP",
        "CREATE CONSTRAINT",
        "DROP CONSTRAINT",
        "CREATE INDEX",
        "DROP INDEX"
    };

    public QueryService(
        IDriver driver,
        string database,
        ILogger<QueryService> logger)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<QueryResult> ExecuteCypherQueryAsync(
        string cypherQuery,
        Dictionary<string, object>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(cypherQuery))
        {
            throw new ArgumentException("Cypher query cannot be null or empty", nameof(cypherQuery));
        }

        // Validate query for security
        var validationResult = await ValidateQueryAsync(cypherQuery);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors);
            throw new UnauthorizedQueryException(
                cypherQuery,
                $"Query validation failed: {errors}");
        }

        var session = _driver.AsyncSession(config => config.WithDatabase(_database));
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Executing Cypher query");
            _logger.LogDebug("Query: {Query}", cypherQuery);

            var cursor = await session.RunAsync(
                cypherQuery,
                parameters ?? new Dictionary<string, object>());

            var records = await cursor.ToListAsync();
            stopwatch.Stop();

            // Get column names
            var columns = records.Any()
                ? records[0].Keys.ToList()
                : new List<string>();

            // Convert records to dictionaries
            var results = records.Select(record =>
            {
                var dict = new Dictionary<string, object>();
                foreach (var key in record.Keys)
                {
                    dict[key] = ConvertNeo4jValue(record[key]);
                }
                return dict;
            }).ToList();

            _logger.LogInformation(
                "Query executed successfully. Records: {Count}, Time: {Time}ms",
                results.Count,
                stopwatch.ElapsedMilliseconds);

            return new QueryResult
            {
                Results = results,
                RecordCount = results.Count,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                Columns = columns
            };
        }
        catch (Neo4jException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Neo4j error executing Cypher query");
            throw new CypherQueryException(
                cypherQuery,
                $"Neo4j error: {ex.Message}",
                ex);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error executing Cypher query");
            throw new CypherQueryException(
                cypherQuery,
                $"Unexpected error: {ex.Message}",
                ex);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<QueryValidationResult> ValidateQueryAsync(string cypherQuery)
    {
        if (string.IsNullOrWhiteSpace(cypherQuery))
        {
            return new QueryValidationResult
            {
                IsValid = false,
                Errors = new List<string> { "Query cannot be null or empty" }
            };
        }

        var result = new QueryValidationResult { IsValid = true };

        // Check for forbidden operations
        var upperQuery = cypherQuery.ToUpperInvariant();
        foreach (var forbiddenOp in ForbiddenOperations)
        {
            if (upperQuery.Contains(forbiddenOp))
            {
                result.IsValid = false;
                result.Errors.Add($"Forbidden operation: {forbiddenOp}");
            }
        }

        // Check for potential injection patterns
        if (ContainsSqlInjectionPatterns(cypherQuery))
        {
            result.IsValid = false;
            result.Errors.Add("Query contains potential injection patterns");
        }

        // Warn about complex queries
        if (cypherQuery.Length > 5000)
        {
            result.Warnings.Add("Query is very long and may impact performance");
        }

        // Check for LIMIT clause
        if (!upperQuery.Contains("LIMIT") && upperQuery.Contains("RETURN"))
        {
            result.Warnings.Add("Consider adding LIMIT clause to prevent large result sets");
        }

        return await Task.FromResult(result);
    }

    /// <inheritdoc/>
    public async Task<List<GraphPath>> FindPathsAsync(
        string fromEntityId,
        string toEntityId,
        int maxDepth = 5,
        string[]? relationshipTypes = null)
    {
        if (string.IsNullOrWhiteSpace(fromEntityId))
        {
            throw new ArgumentException("From entity ID cannot be null or empty", nameof(fromEntityId));
        }

        if (string.IsNullOrWhiteSpace(toEntityId))
        {
            throw new ArgumentException("To entity ID cannot be null or empty", nameof(toEntityId));
        }

        if (maxDepth < 1 || maxDepth > 10)
        {
            throw new ArgumentException("Max depth must be between 1 and 10", nameof(maxDepth));
        }

        var session = _driver.AsyncSession(config => config.WithDatabase(_database));

        try
        {
            _logger.LogInformation(
                "Finding paths from {FromId} to {ToId} (max depth: {Depth})",
                fromEntityId, toEntityId, maxDepth);

            // Build relationship type filter
            var relTypeFilter = relationshipTypes != null && relationshipTypes.Length > 0
                ? ":" + string.Join("|", relationshipTypes)
                : "";

            var query = $@"
                MATCH path = (from)-[{relTypeFilter}*1..{maxDepth}]->(to)
                WHERE from.id = $fromId AND to.id = $toId
                RETURN path,
                       [node IN nodes(path) | node.id] AS entityIds,
                       [rel IN relationships(path) | type(rel)] AS relationshipTypes,
                       length(path) AS pathLength
                ORDER BY pathLength ASC
                LIMIT 10
            ";

            var parameters = new Dictionary<string, object>
            {
                { "fromId", fromEntityId },
                { "toId", toEntityId }
            };

            var cursor = await session.RunAsync(query, parameters);
            var records = await cursor.ToListAsync();

            var paths = records.Select(record =>
            {
                var entityIds = record["entityIds"].As<List<object>>()
                    .Select(id => id.ToString()!).ToList();

                var relTypes = record["relationshipTypes"].As<List<object>>()
                    .Select(t => t.ToString()!).ToList();

                var pathLength = record["pathLength"].As<int>();

                var path = record["path"].As<Neo4j.Driver.IPath>();

                return new GraphPath
                {
                    EntityIds = entityIds,
                    RelationshipTypes = relTypes,
                    Length = pathLength,
                    FullPath = ConvertPathToDictionary(path)
                };
            }).ToList();

            _logger.LogInformation(
                "Found {Count} paths from {FromId} to {ToId}",
                paths.Count, fromEntityId, toEntityId);

            return paths;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding paths");
            throw new CypherQueryException(
                "path_query",
                $"Failed to find paths: {ex.Message}",
                ex);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<GraphNeighborhood> GetNeighborhoodAsync(
        string entityId,
        int depth = 1,
        string[]? relationshipTypes = null)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty", nameof(entityId));
        }

        if (depth < 1 || depth > 5)
        {
            throw new ArgumentException("Depth must be between 1 and 5", nameof(depth));
        }

        var session = _driver.AsyncSession(config => config.WithDatabase(_database));

        try
        {
            _logger.LogInformation(
                "Getting neighborhood for {EntityId} (depth: {Depth})",
                entityId, depth);

            // Build relationship type filter
            var relTypeFilter = relationshipTypes != null && relationshipTypes.Length > 0
                ? ":" + string.Join("|", relationshipTypes)
                : "";

            var query = $@"
                MATCH (center)
                WHERE center.id = $entityId
                OPTIONAL MATCH path = (center)-[{relTypeFilter}*1..{depth}]-(neighbor)
                WITH center, collect(DISTINCT neighbor) AS neighbors, collect(DISTINCT path) AS paths
                RETURN center,
                       neighbors,
                       [p IN paths | relationships(p)] AS allRels
            ";

            var parameters = new Dictionary<string, object>
            {
                { "entityId", entityId }
            };

            var cursor = await session.RunAsync(query, parameters);
            var records = await cursor.ToListAsync();
            var result = records.Count > 0 ? records[0] : null;

            if (result == null)
            {
                throw new EntityNotFoundException(entityId);
            }

            var center = result["center"].As<Neo4j.Driver.INode>();
            var neighbors = result["neighbors"].As<List<Neo4j.Driver.INode>>();

            var neighborhood = new GraphNeighborhood
            {
                CenterEntityId = entityId,
                Depth = depth,
                Entities = new List<Dictionary<string, object>>
                {
                    NodeToDictionary(center)
                }
            };

            // Add all neighbors
            neighborhood.Entities.AddRange(neighbors.Select(NodeToDictionary));

            neighborhood.EntityCount = neighborhood.Entities.Count;
            neighborhood.RelationshipCount = 0;

            _logger.LogInformation(
                "Retrieved neighborhood for {EntityId}: {EntityCount} entities",
                entityId, neighborhood.EntityCount);

            return neighborhood;
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting neighborhood");
            throw new CypherQueryException(
                "neighborhood_query",
                $"Failed to get neighborhood: {ex.Message}",
                ex);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    #region Helper Methods

    /// <summary>
    /// Converts Neo4j value to C# object
    /// </summary>
    private object ConvertNeo4jValue(object value)
    {
        return value switch
        {
            Neo4j.Driver.INode node => NodeToDictionary(node),
            Neo4j.Driver.IRelationship rel => RelationshipToDictionary(rel),
            Neo4j.Driver.IPath path => ConvertPathToDictionary(path),
            IList<object> list => list.Select(ConvertNeo4jValue).ToList(),
            IDictionary<string, object> dict => dict.ToDictionary(
                kvp => kvp.Key,
                kvp => ConvertNeo4jValue(kvp.Value)),
            _ => value
        };
    }

    /// <summary>
    /// Converts Neo4j node to dictionary
    /// </summary>
    private Dictionary<string, object> NodeToDictionary(Neo4j.Driver.INode node)
    {
        var dict = new Dictionary<string, object>
        {
            { "id", node.Properties.ContainsKey("id") ? node.Properties["id"] : node.Id },
            { "labels", node.Labels.ToList() },
            { "properties", new Dictionary<string, object>(node.Properties) }
        };

        return dict;
    }

    /// <summary>
    /// Converts Neo4j relationship to dictionary
    /// </summary>
    private Dictionary<string, object> RelationshipToDictionary(Neo4j.Driver.IRelationship rel)
    {
        var dict = new Dictionary<string, object>
        {
            { "id", rel.Id },
            { "type", rel.Type },
            { "properties", new Dictionary<string, object>(rel.Properties) }
        };

        return dict;
    }

    /// <summary>
    /// Converts Neo4j path to dictionary
    /// </summary>
    private Dictionary<string, object> ConvertPathToDictionary(Neo4j.Driver.IPath path)
    {
        var dict = new Dictionary<string, object>
        {
            { "nodes", path.Nodes.Select(NodeToDictionary).ToList() },
            { "relationships", path.Relationships.Select(RelationshipToDictionary).ToList() },
            { "length", path.Relationships.Count }
        };

        return dict;
    }

    /// <summary>
    /// Checks for SQL injection patterns
    /// </summary>
    private bool ContainsSqlInjectionPatterns(string query)
    {
        // Basic pattern matching for common injection attempts
        var patterns = new[]
        {
            @";\s*DROP",
            @";\s*DELETE",
            @"--\s*$",
            @"/\*.*\*/",
            @"UNION\s+SELECT",
            @"xp_cmdshell"
        };

        return patterns.Any(pattern =>
            Regex.IsMatch(query, pattern, RegexOptions.IgnoreCase));
    }

    #endregion
}
