namespace Binah.Ontology.Models.SupportModels;

/// <summary>
/// Contact information for a person
/// </summary>
public class ContactInfo
{
    public List<string> Emails { get; set; } = new();
    public List<string> Phones { get; set; } = new();
}