namespace Binah.Ontology.Models.Lineage;

/// <summary>
/// Individual property change
/// </summary>
public class PropertyChange
{
    public string PropertyName { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
}