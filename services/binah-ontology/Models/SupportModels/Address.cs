namespace Binah.Ontology.Models.SupportModels;

// <summary>
/// Represents a physical address
/// </summary>
public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Formatted { get; set; } = string.Empty;
}