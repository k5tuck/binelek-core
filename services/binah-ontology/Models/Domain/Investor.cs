
using Binah.Ontology.Models.Base;
namespace Binah.Ontology.Models.Domain;

public class Investor : Entity
{
    public Investor()
    {
        Type = "Investor";
    }

    public string Name
    {
        get => Properties.TryGetValue("name", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["name"] = value;
    }

    public string InvestorType
    {
        get => Properties.TryGetValue("investor_type", out var value) ? value?.ToString() ?? "individual" : "individual";
        set => Properties["investor_type"] = value;
    }

    public decimal TotalInvested
    {
        get => Properties.TryGetValue("total_invested", out var value) ? Convert.ToDecimal(value) : 0m;
        set => Properties["total_invested"] = value;
    }

    public string? RiskTolerance
    {
        get => Properties.TryGetValue("risk_tolerance", out var value) ? value?.ToString() : null;
        set => Properties["risk_tolerance"] = value;
    }

    public decimal? TargetROI
    {
        get => Properties.TryGetValue("target_roi", out var value) ? Convert.ToDecimal(value) : null;
        set => Properties["target_roi"] = value;
    }
}