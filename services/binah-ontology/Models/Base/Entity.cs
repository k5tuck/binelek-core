using Binah.Ontology.Models.Exceptions;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Binah.Ontology.Models.Base;

/// <summary>
    /// Base entity model representing a node in the ontology graph
    /// </summary>
    public class Entity
    {
        [JsonPropertyName("id")]
        /// <summary>Unique entity identifier</summary>
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        /// <summary>Entity type (e.g., Property, Entity, Person, Zone, Document)</summary>
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("properties")]
        /// <summary>Dynamic properties specific to the entity type</summary>
        public Dictionary<string, object> Properties { get; set; } = new();

        [JsonPropertyName("version")]
        /// <summary>Entity version for lineage tracking (e.g., "1.0", "2.3")</summary>
        public string Version { get; set; } = "1.0";

        [JsonPropertyName("createdAt")]
        /// <summary>Timestamp when entity was created</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("updatedAt")]
        /// <summary>Timestamp when entity was last updated</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("source")]
        /// <summary>Source system that created this entity</summary>
        public string Source { get; set; } = "Binah.Ontology";

        [JsonPropertyName("metadata")]
        /// <summary>Additional metadata (tags, enrichment data, etc.)</summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>User or system identifier who created the entity</summary>
        public string? CreatedBy { get; set; }

        /// <summary>User or system identifier who last updated the entity</summary>
        public string? UpdatedBy { get; set; }

        /// <summary>Tenant ID for multi-tenancy support</summary>
        public string? TenantId { get; set; }

        /// <summary>Soft delete flag</summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>Timestamp when entity was deleted (if applicable)</summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>User or system identifier who deleted the entity</summary>
        public string? DeletedBy { get; set; }

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

                if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
                    return (T)(object)Convert.ToBoolean(value);

                // Handle complex objects via JSON serialization
                if (value is System.Text.Json.JsonElement jsonElement)
                {
                    return System.Text.Json.JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                throw new EntityUpdateException(
                    Id,
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

                UpdatedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                throw new EntityUpdateException(
                    Id,
                    $"Failed to set property '{key}' with value '{value}'",
                    ex);
            }
        }
    }
