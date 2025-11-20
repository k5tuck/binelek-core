namespace Binah.Ontology.Models.SupportModels;

/// <summary>
/// Risk assessment scores for zones
/// </summary>
public class RiskScores
{
    public double? Flood { get; set; }
    public double? Seismic { get; set; }
    public double? Environmental { get; set; }
}