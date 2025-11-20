namespace Binah.Ontology.Models.SupportModels;

/// <summary>
/// Professional credentials (licenses, broker IDs, etc.)
/// </summary>
public class Credentials
{
    public List<string> Licenses { get; set; } = new();
    public string? BrokerId { get; set; }
}