namespace Binah.API.Models;

/// <summary>
/// White-label branding configuration for a licensee
/// Allows licensees to customize the platform's appearance
/// </summary>
public class WhiteLabelConfig
{
    /// <summary>
    /// Unique configuration ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Licensee this configuration belongs to
    /// </summary>
    public Guid LicenseeId { get; set; }

    /// <summary>
    /// Company name displayed in the UI
    /// </summary>
    public required string CompanyName { get; set; }

    /// <summary>
    /// URL to company logo (displayed in header, login page, etc.)
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// URL to favicon
    /// </summary>
    public string? FaviconUrl { get; set; }

    /// <summary>
    /// Custom domain for white-label deployment
    /// Example: platform.acme.com
    /// </summary>
    public string? CustomDomain { get; set; }

    /// <summary>
    /// Theme colors for the application
    /// </summary>
    public required ThemeColors Colors { get; set; }

    /// <summary>
    /// Custom text/labels for UI elements
    /// </summary>
    public Dictionary<string, string> CustomText { get; set; } = new();

    /// <summary>
    /// Font configuration
    /// </summary>
    public FontConfig Fonts { get; set; } = new();

    /// <summary>
    /// When this configuration was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this configuration was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Theme color configuration
/// Following shadcn/ui color system conventions
/// </summary>
public class ThemeColors
{
    /// <summary>
    /// Primary brand color (buttons, links, etc.)
    /// </summary>
    public required string Primary { get; set; }

    /// <summary>
    /// Secondary brand color (accents, highlights)
    /// </summary>
    public required string Secondary { get; set; }

    /// <summary>
    /// Background color
    /// </summary>
    public required string Background { get; set; }

    /// <summary>
    /// Foreground/text color
    /// </summary>
    public required string Foreground { get; set; }

    /// <summary>
    /// Muted elements (disabled states, placeholders)
    /// </summary>
    public required string Muted { get; set; }

    /// <summary>
    /// Accent color (call-to-action elements)
    /// </summary>
    public required string Accent { get; set; }

    /// <summary>
    /// Destructive actions (delete, error states)
    /// </summary>
    public string Destructive { get; set; } = "#EF4444";

    /// <summary>
    /// Border color
    /// </summary>
    public string Border { get; set; } = "#E5E7EB";

    /// <summary>
    /// Input field background
    /// </summary>
    public string Input { get; set; } = "#F9FAFB";

    /// <summary>
    /// Focus ring color
    /// </summary>
    public string Ring { get; set; } = "#3B82F6";

    /// <summary>
    /// Create default color scheme (Binelek blue/purple)
    /// </summary>
    public static ThemeColors CreateDefault()
    {
        return new ThemeColors
        {
            Primary = "#3B82F6",      // Blue
            Secondary = "#8B5CF6",    // Purple
            Background = "#FFFFFF",   // White
            Foreground = "#1F2937",   // Dark gray
            Muted = "#F3F4F6",        // Light gray
            Accent = "#10B981",       // Green
            Destructive = "#EF4444",  // Red
            Border = "#E5E7EB",       // Border gray
            Input = "#F9FAFB",        // Input gray
            Ring = "#3B82F6"          // Focus blue
        };
    }
}

/// <summary>
/// Font configuration for typography
/// </summary>
public class FontConfig
{
    /// <summary>
    /// Font family for headings
    /// </summary>
    public string Heading { get; set; } = "Inter";

    /// <summary>
    /// Font family for body text
    /// </summary>
    public string Body { get; set; } = "Inter";

    /// <summary>
    /// Create default font configuration
    /// </summary>
    public static FontConfig CreateDefault()
    {
        return new FontConfig
        {
            Heading = "Inter",
            Body = "Inter"
        };
    }
}

/// <summary>
/// DTO for creating/updating white-label configuration
/// </summary>
public class WhiteLabelConfigDto
{
    public Guid? Id { get; set; }
    public Guid LicenseeId { get; set; }
    public required string CompanyName { get; set; }
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? CustomDomain { get; set; }
    public required ThemeColors Colors { get; set; }
    public Dictionary<string, string>? CustomText { get; set; }
    public FontConfig? Fonts { get; set; }
}

/// <summary>
/// Request to update white-label configuration
/// </summary>
public class UpdateWhiteLabelConfigRequest
{
    public string? CompanyName { get; set; }
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? CustomDomain { get; set; }
    public ThemeColors? Colors { get; set; }
    public Dictionary<string, string>? CustomText { get; set; }
    public FontConfig? Fonts { get; set; }
}
