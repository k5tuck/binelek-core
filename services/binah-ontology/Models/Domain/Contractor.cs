
using Binah.Ontology.Models.Base;
namespace Binah.Ontology.Models.Domain;

public class Contractor : Entity
{
    public Contractor()
    {
        Type = "Contractor";
    }

    public string Name
    {
        get => Properties.TryGetValue("name", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["name"] = value;
    }

    public double Rating
    {
        get => Properties.TryGetValue("rating", out var value) ? Convert.ToDouble(value) : 0.0;
        set => Properties["rating"] = value;
    }

    public List<string> Specialties
    {
        get => Properties.TryGetValue("specialties", out var value) 
            ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(value?.ToString() ?? "[]") ?? new List<string>()
            : new List<string>();
        set => Properties["specialties"] = System.Text.Json.JsonSerializer.Serialize(value);
    }

    public List<string> Certifications
    {
        get => Properties.TryGetValue("certifications", out var value) 
            ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(value?.ToString() ?? "[]") ?? new List<string>()
            : new List<string>();
        set => Properties["certifications"] = System.Text.Json.JsonSerializer.Serialize(value);
    }

    public double? ReliabilityScore
    {
        get => Properties.TryGetValue("reliability_score", out var value) ? Convert.ToDouble(value) : null;
        set => Properties["reliability_score"] = value;
    }
}