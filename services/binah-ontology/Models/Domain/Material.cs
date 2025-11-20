
using Binah.Ontology.Models.Base;
namespace Binah.Ontology.Models.Domain;

public class Material : Entity
{
    public Material()
    {
        Type = "Material";
    }

    public string Name
    {
        get => Properties.TryGetValue("name", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["name"] = value;
    }

    public string Category
    {
        get => Properties.TryGetValue("category", out var value) ? value?.ToString() ?? "general" : "general";
        set => Properties["category"] = value;
    }

    public decimal UnitCost
    {
        get => Properties.TryGetValue("unit_cost", out var value) ? Convert.ToDecimal(value) : 0m;
        set => Properties["unit_cost"] = value;
    }

    public string Unit
    {
        get => Properties.TryGetValue("unit", out var value) ? value?.ToString() ?? "each" : "each";
        set => Properties["unit"] = value;
    }
}