using Binah.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;

namespace Binah.Ontology.Controllers;

[ApiController]
[Route("api/query")]
public class QueryController : ControllerBase
{
    private readonly IDriver _neo4jDriver;
    private readonly ILogger<QueryController> _logger;

    public QueryController(IDriver neo4jDriver, ILogger<QueryController> logger)
    {
        _neo4jDriver = neo4jDriver;
        _logger = logger;
    }

    /// <summary>
    /// Execute a Cypher query against the knowledge graph
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult<QueryExecutionResult>> Execute([FromBody] QueryExecutionRequest request)
    {
        var tenantId = TenantContext.GetRequiredTenantId();

        _logger.LogInformation("Executing query for tenant {TenantId}", tenantId);

        try
        {
            // Build the query with tenant isolation
            var cypherQuery = BuildQuery(request, tenantId);

            await using var session = _neo4jDriver.AsyncSession();
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var records = new List<Dictionary<string, object>>();
                var cursor = await tx.RunAsync(cypherQuery.Query, cypherQuery.Parameters);

                while (await cursor.FetchAsync())
                {
                    var record = new Dictionary<string, object>();
                    foreach (var key in cursor.Current.Keys)
                    {
                        var value = cursor.Current[key];
                        record[key] = ConvertNeo4jValue(value);
                    }
                    records.Add(record);
                }

                return records;
            });

            return Ok(new QueryExecutionResult
            {
                Success = true,
                Data = result,
                RowCount = result.Count,
                ExecutedQuery = cypherQuery.Query
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query execution failed for tenant {TenantId}", tenantId);
            return BadRequest(new QueryExecutionResult
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Preview a query (limit to 100 rows)
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult<QueryExecutionResult>> Preview([FromBody] QueryExecutionRequest request)
    {
        // Force limit for preview
        request.Limit = Math.Min(request.Limit ?? 100, 100);
        return await Execute(request);
    }

    /// <summary>
    /// Get available entity types (tables equivalent)
    /// </summary>
    [HttpGet("entity-types")]
    public async Task<ActionResult<List<string>>> GetEntityTypes()
    {
        var tenantId = TenantContext.GetRequiredTenantId();

        try
        {
            await using var session = _neo4jDriver.AsyncSession();
            var result = await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(
                    "MATCH (n {tenantId: $tenantId}) RETURN DISTINCT labels(n) as labels",
                    new { tenantId });

                var labels = new HashSet<string>();
                while (await cursor.FetchAsync())
                {
                    var nodeLabels = cursor.Current["labels"].As<List<string>>();
                    foreach (var label in nodeLabels)
                    {
                        labels.Add(label);
                    }
                }
                return labels.ToList();
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get entity types for tenant {TenantId}", tenantId);
            return BadRequest(new { error = ex.Message });
        }
    }

    private (string Query, Dictionary<string, object> Parameters) BuildQuery(QueryExecutionRequest request, string tenantId)
    {
        var parameters = new Dictionary<string, object>
        {
            { "tenantId", tenantId }
        };

        // If raw Cypher query provided, add tenant filter
        if (!string.IsNullOrEmpty(request.CypherQuery))
        {
            // Security: Inject tenant filter into MATCH clauses
            var query = request.CypherQuery;

            // Simple tenant injection - in production, use a proper Cypher parser
            if (!query.Contains("tenantId"))
            {
                query = query.Replace("MATCH (", "MATCH (n {tenantId: $tenantId}) WITH n MATCH (");
            }

            if (request.Limit.HasValue)
            {
                if (!query.ToUpper().Contains("LIMIT"))
                {
                    query += $" LIMIT {request.Limit}";
                }
            }

            return (query, parameters);
        }

        // Build query from visual conditions
        var matchClause = $"MATCH (n:{request.EntityType ?? "Entity"} {{tenantId: $tenantId}})";
        var whereClauses = new List<string>();

        if (request.Conditions != null)
        {
            for (int i = 0; i < request.Conditions.Count; i++)
            {
                var cond = request.Conditions[i];
                var paramName = $"p{i}";

                var whereClause = cond.Operator.ToUpper() switch
                {
                    "=" => $"n.{cond.Field} = ${paramName}",
                    "!=" => $"n.{cond.Field} <> ${paramName}",
                    ">" => $"n.{cond.Field} > ${paramName}",
                    "<" => $"n.{cond.Field} < ${paramName}",
                    ">=" => $"n.{cond.Field} >= ${paramName}",
                    "<=" => $"n.{cond.Field} <= ${paramName}",
                    "LIKE" => $"n.{cond.Field} =~ ${paramName}",
                    "IN" => $"n.{cond.Field} IN ${paramName}",
                    _ => $"n.{cond.Field} = ${paramName}"
                };

                whereClauses.Add(whereClause);

                // Handle special operators
                if (cond.Operator.ToUpper() == "LIKE")
                {
                    parameters[paramName] = $".*{cond.Value}.*";
                }
                else if (cond.Operator.ToUpper() == "IN")
                {
                    parameters[paramName] = cond.Value.Split(',').Select(v => v.Trim()).ToList();
                }
                else
                {
                    parameters[paramName] = cond.Value;
                }
            }
        }

        var query = matchClause;
        if (whereClauses.Count > 0)
        {
            query += " WHERE " + string.Join(" AND ", whereClauses);
        }
        query += " RETURN n";

        if (request.Limit.HasValue)
        {
            query += $" LIMIT {request.Limit}";
        }

        if (request.Offset.HasValue)
        {
            query += $" SKIP {request.Offset}";
        }

        return (query, parameters);
    }

    private object ConvertNeo4jValue(object value)
    {
        return value switch
        {
            INode node => new Dictionary<string, object>
            {
                { "id", node.ElementId },
                { "labels", node.Labels },
                { "properties", node.Properties.ToDictionary(kv => kv.Key, kv => kv.Value) }
            },
            IRelationship rel => new Dictionary<string, object>
            {
                { "id", rel.ElementId },
                { "type", rel.Type },
                { "properties", rel.Properties.ToDictionary(kv => kv.Key, kv => kv.Value) }
            },
            IPath path => new Dictionary<string, object>
            {
                { "nodes", path.Nodes.Select(n => ConvertNeo4jValue(n)).ToList() },
                { "relationships", path.Relationships.Select(r => ConvertNeo4jValue(r)).ToList() }
            },
            _ => value
        };
    }
}

public class QueryExecutionRequest
{
    public string? CypherQuery { get; set; }
    public string? EntityType { get; set; }
    public List<QueryCondition>? Conditions { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}

public class QueryCondition
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "=";
    public string Value { get; set; } = string.Empty;
}

public class QueryExecutionResult
{
    public bool Success { get; set; }
    public List<Dictionary<string, object>>? Data { get; set; }
    public int RowCount { get; set; }
    public string? ExecutedQuery { get; set; }
    public string? Error { get; set; }
}
