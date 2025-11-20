using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Binah.Ontology.Models;

/// <summary>
/// Represents a freeform canvas for data composition and relationship visualization
/// </summary>
public class Canvas
{
    /// <summary>
    /// Unique identifier for the canvas
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Tenant ID for multi-tenancy isolation
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Display name for the canvas
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the canvas purpose
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Collection of entities placed on the canvas
    /// </summary>
    public List<CanvasEntity> Entities { get; set; } = new();

    /// <summary>
    /// Collection of connections between entities on the canvas
    /// </summary>
    public List<CanvasConnection> Connections { get; set; } = new();

    /// <summary>
    /// Current viewport settings (pan and zoom)
    /// </summary>
    public CanvasViewport Viewport { get; set; } = new();

    /// <summary>
    /// Whether the canvas is shared with other users
    /// </summary>
    public bool IsShared { get; set; } = false;

    /// <summary>
    /// List of user IDs the canvas is shared with
    /// </summary>
    public List<Guid> SharedWith { get; set; } = new();

    /// <summary>
    /// Timestamp when the canvas was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the canvas was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID who created the canvas
    /// </summary>
    public Guid CreatedBy { get; set; }
}

/// <summary>
/// Represents an entity placed on the canvas with position and visual properties
/// </summary>
public class CanvasEntity
{
    /// <summary>
    /// Unique identifier for this canvas entity instance
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the actual entity ID in the ontology
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Type of the entity (e.g., "Property", "Owner", "Transaction")
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Display label for the entity on the canvas
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// X coordinate position on the canvas
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Y coordinate position on the canvas
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Width of the entity node on the canvas
    /// </summary>
    public double Width { get; set; } = 200;

    /// <summary>
    /// Height of the entity node on the canvas
    /// </summary>
    public double Height { get; set; } = 100;

    /// <summary>
    /// Color for the entity node (hex format)
    /// </summary>
    public string Color { get; set; } = "#3b82f6";
}

/// <summary>
/// Represents a connection between two entities on the canvas
/// </summary>
public class CanvasConnection
{
    /// <summary>
    /// Unique identifier for this connection
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID of the source canvas entity
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// ID of the target canvas entity
    /// </summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// Optional label for the connection (relationship type)
    /// </summary>
    public string? Label { get; set; }
}

/// <summary>
/// Represents the viewport state of the canvas (pan and zoom)
/// </summary>
public class CanvasViewport
{
    /// <summary>
    /// X offset of the viewport
    /// </summary>
    public double X { get; set; } = 0;

    /// <summary>
    /// Y offset of the viewport
    /// </summary>
    public double Y { get; set; } = 0;

    /// <summary>
    /// Zoom level of the viewport
    /// </summary>
    public double Zoom { get; set; } = 1;
}
