namespace Binah.Ontology.Models;

/// <summary>
/// Complete schema definition for a tenant
/// </summary>
public class SchemaDefinition
{
    public string TenantId { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime LastModified { get; set; }
    public List<EntitySchema> Entities { get; set; } = new();
    public List<RelationshipSchema> Relationships { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Schema definition for a single entity type
/// </summary>
public class EntitySchema
{
    public string EntityType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<PropertySchema> Properties { get; set; } = new();
    public List<ValidationRule> ValidationRules { get; set; } = new();
    public UIConfiguration UIConfig { get; set; } = new();
    public bool IsCore { get; set; }
    public string? ExtendsEntity { get; set; }
}

/// <summary>
/// Schema definition for an entity property
/// </summary>
public class PropertySchema
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Required { get; set; }
    public bool Indexed { get; set; }
    public object? DefaultValue { get; set; }
    public string? Description { get; set; }
    public List<string>? EnumValues { get; set; }
    public Dictionary<string, object>? Constraints { get; set; }
}

/// <summary>
/// Schema definition for relationships between entities
/// </summary>
public class RelationshipSchema
{
    public string Type { get; set; } = string.Empty;
    public string FromEntity { get; set; } = string.Empty;
    public string ToEntity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string Cardinality { get; set; } = "one-to-many"; // one-to-one, one-to-many, many-to-many
    public List<PropertySchema>? Properties { get; set; }
}

/// <summary>
/// Validation rule for entity properties
/// </summary>
public class ValidationRule
{
    public string PropertyName { get; set; } = string.Empty;
    public string ValidatorType { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// UI configuration for entity rendering
/// </summary>
public class UIConfiguration
{
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public List<string>? DisplayFields { get; set; }
    public List<string>? SearchableFields { get; set; }
    public List<FormFieldConfig>? FormFields { get; set; }
    public List<TableColumnConfig>? TableColumns { get; set; }
    public Dictionary<string, object>? CustomSettings { get; set; }
}

/// <summary>
/// Form field configuration for dynamic forms
/// </summary>
public class FormFieldConfig
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "text"; // text, number, date, select, textarea, etc.
    public bool Required { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public int Order { get; set; }
    public Dictionary<string, object>? ValidationRules { get; set; }
    public List<SelectOption>? Options { get; set; }
}

/// <summary>
/// Table column configuration for dynamic tables
/// </summary>
public class TableColumnConfig
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Sortable { get; set; }
    public bool Filterable { get; set; }
    public string? FormatType { get; set; } // currency, date, percentage, etc.
    public int Width { get; set; }
    public int Order { get; set; }
}

/// <summary>
/// Select option for dropdown fields
/// </summary>
public class SelectOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
