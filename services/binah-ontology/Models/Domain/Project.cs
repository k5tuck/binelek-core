
using Binah.Ontology.Models.Base;
namespace Binah.Ontology.Models.Domain;

public class Project : Entity
{
    public Project()
    {
        Type = "Project";
    }

    public string Name
    {
        get => Properties.TryGetValue("name", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["name"] = value;
    }

    public string Status
    {
        get => Properties.TryGetValue("status", out var value) ? value?.ToString() ?? "planning" : "planning";
        set => Properties["status"] = value;
    }

    public decimal Budget
    {
        get => Properties.TryGetValue("budget", out var value) ? Convert.ToDecimal(value) : 0m;
        set => Properties["budget"] = value;
    }

    public decimal? PredictedCost
    {
        get => Properties.TryGetValue("predicted_cost", out var value) ? Convert.ToDecimal(value) : null;
        set => Properties["predicted_cost"] = value;
    }

    public double? CostConfidence
    {
        get => Properties.TryGetValue("cost_confidence", out var value) ? Convert.ToDouble(value) : null;
        set => Properties["cost_confidence"] = value;
    }

    public DateTime? StartDate
    {
        get => Properties.TryGetValue("start_date", out var value) ? DateTime.Parse(value?.ToString() ?? string.Empty) : null;
        set => Properties["start_date"] = value?.ToString("O");
    }

    public DateTime? EndDate
    {
        get => Properties.TryGetValue("end_date", out var value) ? DateTime.Parse(value?.ToString() ?? string.Empty) : null;
        set => Properties["end_date"] = value?.ToString("O");
    }

    public string? Description
    {
        get => Properties.TryGetValue("description", out var value) ? value?.ToString() : null;
        set => Properties["description"] = value;
    }
}