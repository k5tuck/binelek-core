using Binah.Ontology.Models.Exceptions;
using Binah.Ontology.Models.SupportModels;
using System.Text.Json.Serialization;

namespace Binah.Ontology.Models.Relationship;

/// <summary>
/// Relationship model representing an edge in the ontology graph
/// </summary>
public class Relationship
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("fromEntityId")]
    public string FromEntityId { get; set; } = string.Empty;

    [JsonPropertyName("toEntityId")]
    public string ToEntityId { get; set; } = string.Empty;

    [JsonPropertyName("properties")]
    public Dictionary<string, object> Properties { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }

    [JsonPropertyName("sinceDate")]
    public DateTime? SinceDate { get; set; }

    [JsonPropertyName("sourceRecords")]
    public List<SourceRecord> SourceRecords { get; set; } = new();

    [JsonPropertyName("confidenceScore")]
    public double ConfidenceScore { get; set; } = 1.0;

    [JsonPropertyName("tenantId")]
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets a strongly-typed property value from the Properties dictionary
    /// </summary>
    protected T? GetPropertyValue<T>(string key, T? defaultValue = default)
    {
        if (!Properties.TryGetValue(key, out var value))
            return defaultValue;

        try
        {
            if (value == null)
                return defaultValue;

            if (value is T typedValue)
                return typedValue;

            if (typeof(T) == typeof(string))
                return (T)(object)value.ToString()!;

            if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
                return (T)(object)Convert.ToDecimal(value);

            if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
                return (T)(object)Convert.ToDouble(value);

            if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
                return (T)(object)Convert.ToInt32(value);

            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                if (value is string strValue)
                    return (T)(object)DateTime.Parse(strValue);
                return (T)(object)Convert.ToDateTime(value);
            }

            if (value is System.Text.Json.JsonElement jsonElement)
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            throw new RelationshipCreationException(
                Type,
                $"Failed to convert property '{key}' with value '{value}' to type {typeof(T).Name}",
                ex);
        }
    }

    /// <summary>
    /// Sets a property value in the Properties dictionary
    /// </summary>
    protected void SetPropertyValue<T>(string key, T value)
    {
        try
        {
            if (value == null)
            {
                Properties.Remove(key);
                return;
            }

            if (value is DateTime dateTime)
            {
                Properties[key] = dateTime.ToString("O");
            }
            else
            {
                Properties[key] = value;
            }
        }
        catch (Exception ex)
        {
            throw new RelationshipCreationException(
                Type,
                $"Failed to set property '{key}' with value '{value}'",
                ex);
        }
    }
}