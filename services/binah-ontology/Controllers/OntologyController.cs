using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Binah.Ontology.Services;
using Binah.Ontology.Models.Ontology;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Binah.Ontology.Controllers;

/// <summary>
/// Controller for ontology version management, upload, publish, and rollback
/// </summary>
[ApiController]
[Route("api/tenants/{tenantId}/ontology")]
[Produces("application/json")]
[Authorize]
public class OntologyController : ControllerBase
{
    private readonly IOntologyVersionService _ontologyService;
    private readonly ILogger<OntologyController> _logger;

    public OntologyController(
        IOntologyVersionService ontologyService,
        ILogger<OntologyController> logger)
    {
        _ontologyService = ontologyService ?? throw new ArgumentNullException(nameof(ontologyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates that the tenant ID from JWT matches the route parameter
    /// </summary>
    private bool ValidateTenantAccess(Guid routeTenantId, out string? jwtTenantId)
    {
        jwtTenantId = User.FindFirst("tenant_id")?.Value;

        if (string.IsNullOrEmpty(jwtTenantId))
        {
            return false;
        }

        // Convert route tenant ID to string and compare
        return routeTenantId.ToString() == jwtTenantId || jwtTenantId == "core";
    }

    /// <summary>
    /// Get the currently active ontology version for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>Active ontology version or 404 if none found</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(OntologyVersion), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OntologyVersion>> GetActive([FromRoute] Guid tenantId)
    {
        try
        {
            // Validate tenant access
            if (!ValidateTenantAccess(tenantId, out var jwtTenantId))
            {
                return Unauthorized(new { message = "Tenant ID not found in token or access denied" });
            }

            _logger.LogInformation("Getting active ontology for tenant {TenantId}", tenantId);

            var activeVersion = await _ontologyService.GetActiveAsync(tenantId);

            if (activeVersion == null)
            {
                _logger.LogWarning("No active ontology found for tenant {TenantId}", tenantId);
                return NotFound(new { message = $"No active ontology found for tenant {tenantId}" });
            }

            return Ok(activeVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active ontology for tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "An error occurred while retrieving the active ontology", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all ontology versions for a tenant
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>List of all ontology versions</returns>
    [HttpGet("versions")]
    [ProducesResponseType(typeof(List<OntologyVersion>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<OntologyVersion>>> GetVersions([FromRoute] Guid tenantId)
    {
        try
        {
            // Validate tenant access
            if (!ValidateTenantAccess(tenantId, out var jwtTenantId))
            {
                return Unauthorized(new { message = "Tenant ID not found in token or access denied" });
            }

            _logger.LogInformation("Getting all ontology versions for tenant {TenantId}", tenantId);

            var versions = await _ontologyService.GetVersionsAsync(tenantId);

            return Ok(versions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ontology versions for tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "An error occurred while retrieving ontology versions", error = ex.Message });
        }
    }

    /// <summary>
    /// Upload a new ontology from YAML file
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="file">YAML ontology file</param>
    /// <param name="version">Version number (optional - will auto-increment if not provided)</param>
    /// <param name="description">Version description</param>
    /// <param name="createdBy">User uploading the ontology</param>
    /// <returns>Created ontology version</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(OntologyVersion), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<OntologyVersion>> UploadYaml(
        [FromRoute] Guid tenantId,
        [FromForm] IFormFile file,
        [FromForm] string? version = null,
        [FromForm] string? description = null,
        [FromForm] string? createdBy = null)
    {
        try
        {
            // Validate tenant access
            if (!ValidateTenantAccess(tenantId, out var jwtTenantId))
            {
                return Unauthorized(new { message = "Tenant ID not found in token or access denied" });
            }

            _logger.LogInformation("Uploading YAML ontology for tenant {TenantId}", tenantId);

            // Validate file
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded" });
            }

            if (!file.FileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) &&
                !file.FileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "File must be a YAML file (.yaml or .yml)" });
            }

            // Read file content
            string yamlContent;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                yamlContent = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(yamlContent))
            {
                return BadRequest(new { message = "YAML file is empty" });
            }

            // Parse YAML to object
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            object yamlObject;
            try
            {
                yamlObject = deserializer.Deserialize<object>(yamlContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse YAML file");
                return BadRequest(new { message = "Invalid YAML format", error = ex.Message });
            }

            // Convert to JSON for storage
            var modelJson = JsonSerializer.Serialize(yamlObject, new JsonSerializerOptions { WriteIndented = true });

            // Extract ontology name from YAML
            string ontologyName = file.FileName.Replace(".yaml", "").Replace(".yml", "");
            try
            {
                var yamlDict = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
                if (yamlDict != null)
                {
                    if (yamlDict.TryGetValue("name", out var nameValue))
                    {
                        ontologyName = nameValue?.ToString() ?? ontologyName;
                    }
                    else if (yamlDict.TryGetValue("ontology", out var ontologyObj) && ontologyObj is Dictionary<string, object> ontologyDict)
                    {
                        if (ontologyDict.TryGetValue("name", out var ontNameValue))
                        {
                            ontologyName = ontNameValue?.ToString() ?? ontologyName;
                        }
                    }
                }
            }
            catch
            {
                // If extraction fails, use filename
            }

            // Auto-increment version if not provided
            if (string.IsNullOrWhiteSpace(version))
            {
                var existingVersions = await _ontologyService.GetVersionsAsync(tenantId);
                var latestVersion = existingVersions
                    .Where(v => v.OntologyName == ontologyName)
                    .OrderByDescending(v => v.CreatedAt)
                    .FirstOrDefault();

                if (latestVersion != null)
                {
                    // Try to parse and increment version
                    if (Version.TryParse(latestVersion.Version, out var parsedVersion))
                    {
                        version = $"{parsedVersion.Major}.{parsedVersion.Minor}.{parsedVersion.Build + 1}";
                    }
                    else
                    {
                        version = $"1.0.{existingVersions.Count(v => v.OntologyName == ontologyName) + 1}";
                    }
                }
                else
                {
                    version = "1.0.0";
                }
            }

            // Create ontology version object
            var ontologyVersion = new OntologyVersion
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                OntologyName = ontologyName,
                Version = version,
                Description = description ?? $"Uploaded from {file.FileName}",
                ModelJson = modelJson,
                CreatedBy = createdBy ?? "system",
                CreatedAt = DateTime.UtcNow,
                IsActive = false,
                Branch = "main"
            };

            // Validate the ontology
            var isValid = await _ontologyService.ValidateAsync(ontologyVersion);
            if (!isValid)
            {
                _logger.LogWarning("Ontology validation failed for {Name} v{Version}", ontologyName, version);
                return BadRequest(new
                {
                    message = "Ontology validation failed",
                    ontologyName,
                    version,
                    details = "Check logs for validation errors"
                });
            }

            // Create the ontology version
            var created = await _ontologyService.CreateAsync(ontologyVersion);

            _logger.LogInformation("Successfully uploaded ontology {Name} v{Version} for tenant {TenantId}",
                created.OntologyName, created.Version, tenantId);

            return CreatedAtAction(
                nameof(GetActive),
                new { tenantId },
                created);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Ontology upload failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading ontology for tenant {TenantId}", tenantId);
            return StatusCode(500, new { message = "An error occurred while uploading the ontology", error = ex.Message });
        }
    }

    /// <summary>
    /// Publish (activate) an ontology version
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="request">Publish request containing version to activate</param>
    /// <returns>Success response</returns>
    [HttpPost("publish")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Publish(
        [FromRoute] Guid tenantId,
        [FromBody] PublishOntologyRequest request)
    {
        try
        {
            // Validate tenant access
            if (!ValidateTenantAccess(tenantId, out var jwtTenantId))
            {
                return Unauthorized(new { message = "Tenant ID not found in token or access denied" });
            }

            if (string.IsNullOrWhiteSpace(request.Version))
            {
                return BadRequest(new { message = "Version is required" });
            }

            _logger.LogInformation("Publishing ontology version {Version} for tenant {TenantId}",
                request.Version, tenantId);

            await _ontologyService.PublishAsync(tenantId, request.Version);

            return Ok(new
            {
                message = $"Ontology version {request.Version} published successfully",
                version = request.Version,
                publishedAt = DateTime.UtcNow
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Publish failed: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing ontology version {Version} for tenant {TenantId}",
                request.Version, tenantId);
            return StatusCode(500, new { message = "An error occurred while publishing the ontology", error = ex.Message });
        }
    }

    /// <summary>
    /// Rollback to a previous ontology version (modular rollback with options)
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="request">Rollback request containing version and options</param>
    /// <returns>Success response</returns>
    [HttpPost("rollback")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Rollback(
        [FromRoute] Guid tenantId,
        [FromBody] RollbackOntologyRequest request)
    {
        try
        {
            // Validate tenant access
            if (!ValidateTenantAccess(tenantId, out var jwtTenantId))
            {
                return Unauthorized(new { message = "Tenant ID not found in token or access denied" });
            }

            if (string.IsNullOrWhiteSpace(request.TargetVersion))
            {
                return BadRequest(new { message = "TargetVersion is required" });
            }

            _logger.LogInformation("Rolling back to ontology version {Version} for tenant {TenantId} with options: RollbackCode={RollbackCode}, RollbackDataMigration={RollbackDataMigration}, ValidateData={ValidateData}",
                request.TargetVersion, tenantId, request.RollbackCode, request.RollbackDataMigration, request.ValidateData);

            // Get the target version
            var versions = await _ontologyService.GetVersionsAsync(tenantId);
            var targetVersion = versions.FirstOrDefault(v => v.Version == request.TargetVersion);

            if (targetVersion == null)
            {
                return NotFound(new { message = $"Ontology version {request.TargetVersion} not found" });
            }

            var steps = new List<string>();

            // Step 1: Rollback code (publish the old version)
            if (request.RollbackCode)
            {
                await _ontologyService.PublishAsync(tenantId, request.TargetVersion);
                steps.Add($"Activated ontology version {request.TargetVersion}");
            }

            // Step 2: Rollback data migration (TODO: implement data migration rollback)
            if (request.RollbackDataMigration)
            {
                // TODO: Implement data migration rollback
                // This would involve:
                // 1. Identify schema changes between current and target version
                // 2. Generate reverse migration scripts
                // 3. Execute rollback migrations on Neo4j and SQL Server
                _logger.LogWarning("Data migration rollback requested but not yet implemented");
                steps.Add("Data migration rollback: Not yet implemented (TODO)");
            }

            // Step 3: Validate data (TODO: implement data validation)
            if (request.ValidateData)
            {
                // TODO: Implement data validation
                // This would involve:
                // 1. Validate existing Neo4j data against target ontology schema
                // 2. Report any incompatibilities
                // 3. Option to clean up incompatible data
                _logger.LogWarning("Data validation requested but not yet implemented");
                steps.Add("Data validation: Not yet implemented (TODO)");
            }

            _logger.LogInformation("Rollback completed for tenant {TenantId} to version {Version}",
                tenantId, request.TargetVersion);

            return Ok(new
            {
                message = $"Rollback to version {request.TargetVersion} completed",
                targetVersion = request.TargetVersion,
                rollbackCode = request.RollbackCode,
                rollbackDataMigration = request.RollbackDataMigration,
                validateData = request.ValidateData,
                steps,
                rolledBackAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during rollback to version {Version} for tenant {TenantId}",
                request.TargetVersion, tenantId);
            return StatusCode(500, new { message = "An error occurred during rollback", error = ex.Message });
        }
    }
}

/// <summary>
/// Request model for publishing an ontology version
/// </summary>
public class PublishOntologyRequest
{
    /// <summary>
    /// Version to publish/activate
    /// </summary>
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// Request model for rolling back to a previous ontology version
/// Supports modular rollback with granular control over what gets rolled back
/// </summary>
public class RollbackOntologyRequest
{
    /// <summary>
    /// Target version to rollback to
    /// </summary>
    public string TargetVersion { get; set; } = string.Empty;

    /// <summary>
    /// Rollback the code (activate the target ontology version)
    /// Default: true
    /// </summary>
    public bool RollbackCode { get; set; } = true;

    /// <summary>
    /// Rollback data migrations (reverse schema changes on data)
    /// Default: false (requires explicit opt-in due to data risk)
    /// </summary>
    public bool RollbackDataMigration { get; set; } = false;

    /// <summary>
    /// Validate existing data against target ontology schema
    /// Default: true
    /// </summary>
    public bool ValidateData { get; set; } = true;
}
