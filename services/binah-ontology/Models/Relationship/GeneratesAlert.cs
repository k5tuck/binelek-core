namespace Binah.Ontology.Models.Relationship;

/// <summary>
/// GENERATES_ALERT relationship: Entity generates an Alert
/// Direction: Entity -> Alert
/// </summary>
public class GeneratesAlertRelationship : Relationship
{
    public GeneratesAlertRelationship()
    {
        Type = "GENERATES_ALERT";
    }

    /// <summary>Alert trigger condition</summary>
    public string? TriggerCondition
    {
        get => GetPropertyValue<string>("trigger_condition");
        set => SetPropertyValue("trigger_condition", value);
    }

    /// <summary>Value that triggered the alert</summary>
    public object? TriggerValue
    {
        get => GetPropertyValue<object>("trigger_value");
        set => SetPropertyValue("trigger_value", value);
    }
}