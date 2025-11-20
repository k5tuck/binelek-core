using System.Collections.Generic;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Service interface for executing custom Cypher queries against the graph database
/// </summary>
public interface IQueryService
{
    /// <summary>
    /// Executes a custom Cypher query with parameters
    /// </summary>
    /// <param name="cypherQuery">The Cypher query string</param>
    /// <param name="parameters">Optional query parameters</param>
    /// <returns>Query result containing records, execution time, and metadata</returns>
    /// <exception cref="CypherQueryException">Thrown when query execution fails</exception>
    /// <exception cref="UnauthorizedQueryException">Thrown when query contains forbidden operations</exception>
    Task<QueryResult> ExecuteCypherQueryAsync(
        string cypherQuery,
        Dictionary<string, object>? parameters = null
    );

    /// <summary>
    /// Validates a Cypher query without executing it
    /// </summary>
    /// <param name="cypherQuery">The Cypher query to validate</param>
    /// <returns>Validation result with errors if any</returns>
    Task<QueryValidationResult> ValidateQueryAsync(string cypherQuery);

    /// <summary>
    /// Executes a graph traversal query to find paths between entities
    /// </summary>
    /// <param name="fromEntityId">Starting entity ID</param>
    /// <param name="toEntityId">Target entity ID</param>
    /// <param name="maxDepth">Maximum path depth</param>
    /// <param name="relationshipTypes">Optional filter for relationship types</param>
    /// <returns>List of paths between entities</returns>
    Task<List<GraphPath>> FindPathsAsync(
        string fromEntityId,
        string toEntityId,
        int maxDepth = 5,
        string[]? relationshipTypes = null
    );

    /// <summary>
    /// Gets the neighborhood of an entity (all connected entities within depth)
    /// </summary>
    /// <param name="entityId">The entity ID</param>
    /// <param name="depth">How many hops to traverse</param>
    /// <param name="relationshipTypes">Optional filter for relationship types</param>
    /// <returns>Graph neighborhood result</returns>
    Task<GraphNeighborhood> GetNeighborhoodAsync(
        string entityId,
        int depth = 1,
        string[]? relationshipTypes = null
    );
}

/// <summary>
/// Result of a Cypher query execution
/// </summary>
public class QueryResult
{
    /// <summary>Query result records as dictionaries</summary>
    public List<Dictionary<string, object>> Results { get; set; } = new();

    /// <summary>Number of records returned</summary>
    public int RecordCount { get; set; }

    /// <summary>Query execution time in milliseconds</summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>Column names in result set</summary>
    public List<string> Columns { get; set; } = new();
}

/// <summary>
/// Result of query validation
/// </summary>
public class QueryValidationResult
{
    /// <summary>Whether the query is valid</summary>
    public bool IsValid { get; set; }

    /// <summary>Validation error messages</summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>Validation warnings</summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Represents a path in the graph between two entities
/// </summary>
public class GraphPath
{
    /// <summary>List of entity IDs in the path</summary>
    public List<string> EntityIds { get; set; } = new();

    /// <summary>List of relationship types in the path</summary>
    public List<string> RelationshipTypes { get; set; } = new();

    /// <summary>Path length (number of hops)</summary>
    public int Length { get; set; }

    /// <summary>Full path data including entities and relationships</summary>
    public Dictionary<string, object> FullPath { get; set; } = new();
}

/// <summary>
/// Represents the neighborhood graph around an entity
/// </summary>
public class GraphNeighborhood
{
    /// <summary>Center entity ID</summary>
    public string CenterEntityId { get; set; } = string.Empty;

    /// <summary>All entities in the neighborhood</summary>
    public List<Dictionary<string, object>> Entities { get; set; } = new();

    /// <summary>All relationships in the neighborhood</summary>
    public List<Dictionary<string, object>> Relationships { get; set; } = new();

    /// <summary>Depth of the neighborhood</summary>
    public int Depth { get; set; }

    /// <summary>Total number of entities found</summary>
    public int EntityCount { get; set; }

    /// <summary>Total number of relationships found</summary>
    public int RelationshipCount { get; set; }
}
