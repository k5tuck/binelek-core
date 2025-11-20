using Binah.Ontology.Models.Migrations;
using Binah.Ontology.Models.Ontology;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Binah.Ontology.Services;

/// <summary>
/// Generates migration scripts (SQL + Cypher) from ontology changes
/// Supports both forward migrations and rollback scripts
/// </summary>
public interface IMigrationScriptGenerator
{
    Task<MigrationScripts> GenerateAsync(OntologyVersion from, OntologyVersion to, List<OntologyChange> changes);
    Task<MigrationScripts> GenerateRollbackAsync(OntologyVersion from, OntologyVersion to);
}

public class MigrationScriptGenerator : IMigrationScriptGenerator
{
    private readonly ISqlMigrationGenerator _sqlGenerator;
    private readonly ICypherMigrationGenerator _cypherGenerator;
    private readonly ILogger<MigrationScriptGenerator> _logger;

    public MigrationScriptGenerator(
        ISqlMigrationGenerator sqlGenerator,
        ICypherMigrationGenerator cypherGenerator,
        ILogger<MigrationScriptGenerator> logger)
    {
        _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        _cypherGenerator = cypherGenerator ?? throw new ArgumentNullException(nameof(cypherGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MigrationScripts> GenerateAsync(
        OntologyVersion from,
        OntologyVersion to,
        List<OntologyChange> changes)
    {
        try
        {
            _logger.LogInformation("Generating migration scripts: {FromVersion} → {ToVersion} ({ChangeCount} changes)",
                from.Version, to.Version, changes.Count);

            var scripts = new MigrationScripts
            {
                FromVersion = from.Version,
                ToVersion = to.Version,
                GeneratedAt = DateTime.UtcNow
            };

            // Generate SQL migration
            scripts.SqlScript = await _sqlGenerator.GenerateMigrationAsync(from, to, changes);
            scripts.SqlRollbackScript = await _sqlGenerator.GenerateRollbackAsync(from, to, changes);

            // Generate Cypher migration
            scripts.CypherScript = await _cypherGenerator.GenerateMigrationAsync(from, to, changes);
            scripts.CypherRollbackScript = await _cypherGenerator.GenerateRollbackAsync(from, to, changes);

            _logger.LogInformation("Successfully generated migration scripts: SQL={SqlLength} chars, Cypher={CypherLength} chars",
                scripts.SqlScript.Length, scripts.CypherScript.Length);

            return scripts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating migration scripts: {FromVersion} → {ToVersion}",
                from.Version, to.Version);
            throw;
        }
    }

    public async Task<MigrationScripts> GenerateRollbackAsync(OntologyVersion from, OntologyVersion to)
    {
        try
        {
            _logger.LogInformation("Generating rollback scripts: {FromVersion} → {ToVersion}",
                from.Version, to.Version);

            var scripts = new MigrationScripts
            {
                FromVersion = from.Version,
                ToVersion = to.Version,
                GeneratedAt = DateTime.UtcNow
            };

            // Generate reverse changes
            var reverseChanges = await DetectChangesAsync(to, from);

            scripts.SqlScript = await _sqlGenerator.GenerateMigrationAsync(to, from, reverseChanges);
            scripts.CypherScript = await _cypherGenerator.GenerateMigrationAsync(to, from, reverseChanges);

            _logger.LogInformation("Successfully generated rollback scripts");

            return scripts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating rollback scripts: {FromVersion} → {ToVersion}",
                from.Version, to.Version);
            throw;
        }
    }

    private async Task<List<OntologyChange>> DetectChangesAsync(OntologyVersion from, OntologyVersion to)
    {
        // This would use OntologyDiffEngine to detect changes
        // For now, return empty list
        await Task.CompletedTask;
        return new List<OntologyChange>();
    }
}

/// <summary>
/// Container for migration scripts
/// </summary>
public class MigrationScripts
{
    public string FromVersion { get; set; } = string.Empty;
    public string ToVersion { get; set; } = string.Empty;
    public string SqlScript { get; set; } = string.Empty;
    public string SqlRollbackScript { get; set; } = string.Empty;
    public string CypherScript { get; set; } = string.Empty;
    public string CypherRollbackScript { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}
