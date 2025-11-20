using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Binah.Ontology.Models.Base;
using Microsoft.Extensions.Logging;

namespace Binah.Ontology.Pipelines.DataNetwork;

/// <summary>
/// Domain-agnostic PII scrubber implementation
/// Scrubs PII based on common field patterns and scrubbing level
/// </summary>
public class PiiScrubber : IPiiScrubber
{
    private readonly ILogger<PiiScrubber>? _logger;

    // Common PII field patterns (case-insensitive matching)
    private static readonly HashSet<string> CommonPiiFields = new(StringComparer.OrdinalIgnoreCase)
    {
        // Personal identifiers
        "ssn", "ssn_tin", "tin", "tax_id", "taxid", "social_security_number",
        "passport", "passport_number", "drivers_license", "license_number",
        
        // Contact information
        "email", "email_address", "phone", "phone_number", "mobile", "mobile_phone",
        "address", "street_address", "postal_code", "zip_code", "zip",
        
        // Personal details
        "first_name", "last_name", "full_name", "name", "date_of_birth", "dob",
        "birth_date", "age",
        
        // Financial PII
        "account_number", "bank_account", "credit_card", "card_number",
        "routing_number", "iban", "swift_code",
        
        // Other sensitive data
        "ip_address", "mac_address", "device_id"
    };

    // Fields that should always be removed (encrypted/highly sensitive)
    private static readonly HashSet<string> AlwaysRemoveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "ssn", "ssn_tin", "tin", "tax_id", "taxid", "social_security_number",
        "password", "password_hash", "secret", "api_key", "access_token"
    };

    public PiiScrubber(ILogger<PiiScrubber>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Entity ScrubEntity(Entity entity, string entityType, ScrubbingLevel level)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        // Clone the entity to avoid modifying the original
        var scrubbed = CloneEntity(entity);

        // Apply scrubbing based on level
        switch (level)
        {
            case ScrubbingLevel.Strict:
                ScrubStrict(scrubbed);
                break;
            case ScrubbingLevel.Moderate:
                ScrubModerate(scrubbed);
                break;
            case ScrubbingLevel.Minimal:
                ScrubMinimal(scrubbed);
                break;
        }

        // Always remove encrypted/highly sensitive fields
        RemoveEncryptedFields(scrubbed);

        // Hash entity ID
        if (!string.IsNullOrEmpty(scrubbed.Id))
        {
            scrubbed.Id = HashIdentifier(scrubbed.Id, scrubbed.TenantId);
        }

        // Add scrubbing metadata
        scrubbed.Metadata ??= new Dictionary<string, object>();
        scrubbed.Metadata["scrubbed"] = true;
        scrubbed.Metadata["scrubbing_level"] = level.ToString();
        scrubbed.Metadata["original_tenant_id_hash"] = HashIdentifier(scrubbed.TenantId ?? "");
        scrubbed.Metadata["entity_type"] = entityType;
        scrubbed.Metadata["scrubbed_at"] = DateTime.UtcNow;

        // Remove tenant ID (replaced with hash in metadata)
        scrubbed.TenantId = null;

        _logger?.LogDebug("Scrubbed entity {EntityType} with level {Level}", entityType, level);

        return scrubbed;
    }

    private void ScrubStrict(Entity entity)
    {
        // Remove all PII fields
        var fieldsToRemove = entity.Properties.Keys
            .Where(key => CommonPiiFields.Contains(key))
            .ToList();

        foreach (var field in fieldsToRemove)
        {
            entity.Properties.Remove(field);
        }

        // Generalize dates (convert to year-month only)
        GeneralizeDates(entity);
    }

    private void ScrubModerate(Entity entity)
    {
        // Tokenize PII fields instead of removing
        var fieldsToTokenize = entity.Properties.Keys
            .Where(key => CommonPiiFields.Contains(key) && !AlwaysRemoveFields.Contains(key))
            .ToList();

        foreach (var field in fieldsToTokenize)
        {
            if (entity.Properties.TryGetValue(field, out var value) && value != null)
            {
                entity.Properties[field] = TokenizePii(value);
            }
        }

        // Remove always-remove fields
        foreach (var field in AlwaysRemoveFields)
        {
            entity.Properties.Remove(field);
        }
    }

    private void ScrubMinimal(Entity entity)
    {
        // Only remove highly sensitive fields
        foreach (var field in AlwaysRemoveFields)
        {
            entity.Properties.Remove(field);
        }
    }

    private void RemoveEncryptedFields(Entity entity)
    {
        // Remove fields that are typically encrypted
        var encryptedPatterns = new[] { "encrypted", "hash", "token", "secret", "key" };
        var fieldsToRemove = entity.Properties.Keys
            .Where(key => encryptedPatterns.Any(pattern => 
                key.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var field in fieldsToRemove)
        {
            entity.Properties.Remove(field);
        }
    }

    private void GeneralizeDates(Entity entity)
    {
        var dateFields = new[] { "date", "created_at", "updated_at", "deleted_at", "timestamp" };
        
        foreach (var key in entity.Properties.Keys.ToList())
        {
            if (dateFields.Any(df => key.Contains(df, StringComparison.OrdinalIgnoreCase)))
            {
                if (entity.Properties[key] is DateTime dt)
                {
                    // Generalize to year-month only
                    entity.Properties[key] = new DateTime(dt.Year, dt.Month, 1);
                }
                else if (entity.Properties[key] is string dateStr && 
                         DateTime.TryParse(dateStr, out var parsedDate))
                {
                    entity.Properties[key] = new DateTime(parsedDate.Year, parsedDate.Month, 1).ToString("yyyy-MM");
                }
            }
        }
    }

    private object TokenizePii(object value)
    {
        // Simple tokenization: hash the value
        // In production, you might want to use a reversible tokenization service
        var valueStr = value?.ToString() ?? "";
        return $"TOKEN_{HashIdentifier(valueStr)}";
    }

    private Entity CloneEntity(Entity entity)
    {
        // Deep clone the entity
        var cloned = new Entity
        {
            Id = entity.Id,
            Type = entity.Type,
            Version = entity.Version,
            Properties = new Dictionary<string, object>(entity.Properties),
            Metadata = entity.Metadata != null 
                ? new Dictionary<string, object>(entity.Metadata) 
                : new Dictionary<string, object>(),
            TenantId = entity.TenantId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt,
            DeletedBy = entity.DeletedBy,
            Source = entity.Source
        };

        return cloned;
    }

    private string HashIdentifier(string value, string? salt = null)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        var input = salt != null ? $"{value}:{salt}" : value;
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash)[..16]; // Use first 16 chars for readability
    }
}

