namespace Binah.Ontology.Models.Ontology;

public class EntityDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = "#3b82f6";
    public Position Position { get; set; } = new();
    public List<PropertyDefinition> Properties { get; set; } = new();
}

public class Position
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class PropertyDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
}
