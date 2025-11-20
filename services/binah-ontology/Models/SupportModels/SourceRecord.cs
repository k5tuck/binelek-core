namespace Binah.Ontology.Models.SupportModels;

/// <summary>
/// Represents a source record reference for data provenance
/// </summary>
public class SourceRecord
{
    public string Source { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public DateTime IngestTime { get; set; } = DateTime.UtcNow;
}