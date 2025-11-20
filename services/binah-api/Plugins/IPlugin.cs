namespace Binah.API.Plugins;

/// <summary>
/// Plugin interface for extending Binelek Platform functionality
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Plugin name (must be unique)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Plugin version (semver format)
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Plugin description
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Initialize plugin with context
    /// </summary>
    void Initialize(IPluginContext context);

    /// <summary>
    /// Execute plugin action
    /// </summary>
    Task<PluginResult> ExecuteAsync(PluginRequest request);
}

/// <summary>
/// Plugin context providing access to platform services
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// Service provider for dependency injection
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Current licensee ID
    /// </summary>
    Guid LicenseeId { get; }

    /// <summary>
    /// Event bus for publishing/subscribing to events
    /// </summary>
    IEventBus Events { get; }

    /// <summary>
    /// Logger for plugin
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Plugin configuration
    /// </summary>
    IConfiguration Configuration { get; }
}

/// <summary>
/// Event bus for plugin communication
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribe to events
    /// </summary>
    void Subscribe<T>(Func<T, Task> handler) where T : class;

    /// <summary>
    /// Publish event
    /// </summary>
    Task PublishAsync<T>(T eventData) where T : class;
}

/// <summary>
/// Plugin execution request
/// </summary>
public class PluginRequest
{
    /// <summary>
    /// Action to execute
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// Request parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// User context
    /// </summary>
    public PluginUserContext? UserContext { get; set; }
}

/// <summary>
/// User context for plugin requests
/// </summary>
public class PluginUserContext
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Plugin execution result
/// </summary>
public class PluginResult
{
    /// <summary>
    /// Indicates if execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Result data (can be any type)
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Error message (if Success = false)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static PluginResult SuccessResult(object? data = null)
    {
        return new PluginResult { Success = true, Data = data };
    }

    public static PluginResult ErrorResult(string errorMessage)
    {
        return new PluginResult { Success = false, ErrorMessage = errorMessage };
    }
}

/// <summary>
/// Plugin lifecycle events
/// </summary>
public class PluginLoadedEvent
{
    public required string PluginName { get; set; }
    public required string Version { get; set; }
    public DateTime LoadedAt { get; set; } = DateTime.UtcNow;
}

public class PluginErrorEvent
{
    public required string PluginName { get; set; }
    public required string ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
