using System.Reflection;
using System.Runtime.Loader;

namespace Binah.API.Plugins;

/// <summary>
/// Plugin loader for dynamically loading and managing plugins
/// </summary>
public class PluginLoader
{
    private readonly ILogger<PluginLoader> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<IPlugin> _plugins = new();
    private readonly Dictionary<string, IPlugin> _pluginsByName = new();

    public PluginLoader(ILogger<PluginLoader> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Load plugins from directory
    /// </summary>
    public void LoadPlugins(string pluginsDirectory)
    {
        if (!Directory.Exists(pluginsDirectory))
        {
            _logger.LogWarning("Plugins directory not found: {Directory}", pluginsDirectory);
            return;
        }

        _logger.LogInformation("Loading plugins from: {Directory}", pluginsDirectory);

        // Find all .dll files
        var pluginFiles = Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.AllDirectories);

        foreach (var pluginFile in pluginFiles)
        {
            try
            {
                LoadPlugin(pluginFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from {File}", pluginFile);
            }
        }

        _logger.LogInformation("Loaded {Count} plugins", _plugins.Count);
    }

    /// <summary>
    /// Load single plugin from file
    /// </summary>
    private void LoadPlugin(string pluginPath)
    {
        // Load assembly
        var loadContext = new AssemblyLoadContext(pluginPath, isCollectible: true);
        var assembly = loadContext.LoadFromAssemblyPath(pluginPath);

        // Find types implementing IPlugin
        var pluginTypes = assembly.GetTypes()
            .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var pluginType in pluginTypes)
        {
            try
            {
                // Instantiate plugin
                var plugin = (IPlugin?)Activator.CreateInstance(pluginType);
                if (plugin == null)
                {
                    _logger.LogWarning("Failed to instantiate plugin type: {Type}", pluginType.Name);
                    continue;
                }

                // Check for duplicate names
                if (_pluginsByName.ContainsKey(plugin.Name))
                {
                    _logger.LogWarning("Plugin with name {Name} already loaded, skipping duplicate", plugin.Name);
                    continue;
                }

                // Initialize plugin
                var context = new PluginContext(_serviceProvider, _logger);
                plugin.Initialize(context);

                // Add to loaded plugins
                _plugins.Add(plugin);
                _pluginsByName[plugin.Name] = plugin;

                _logger.LogInformation("Loaded plugin: {Name} v{Version}", plugin.Name, plugin.Version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize plugin type: {Type}", pluginType.Name);
            }
        }
    }

    /// <summary>
    /// Execute plugin by name
    /// </summary>
    public async Task<PluginResult> ExecutePluginAsync(string pluginName, PluginRequest request)
    {
        if (!_pluginsByName.TryGetValue(pluginName, out var plugin))
        {
            throw new PluginNotFoundException(pluginName);
        }

        try
        {
            _logger.LogInformation("Executing plugin {Plugin} action {Action}", pluginName, request.Action);
            return await plugin.ExecuteAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin {Plugin} execution failed", pluginName);
            return PluginResult.ErrorResult($"Plugin execution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all loaded plugins
    /// </summary>
    public IReadOnlyList<IPlugin> GetLoadedPlugins() => _plugins.AsReadOnly();

    /// <summary>
    /// Get plugin by name
    /// </summary>
    public IPlugin? GetPlugin(string name) => _pluginsByName.GetValueOrDefault(name);
}

/// <summary>
/// Plugin context implementation
/// </summary>
internal class PluginContext : IPluginContext
{
    public IServiceProvider Services { get; }
    public Guid LicenseeId => Guid.Empty; // TODO: Get from context
    public IEventBus Events { get; }
    public ILogger Logger { get; }
    public IConfiguration Configuration { get; }

    public PluginContext(IServiceProvider serviceProvider, ILogger logger)
    {
        Services = serviceProvider;
        Logger = logger;
        Events = new SimpleEventBus();
        Configuration = serviceProvider.GetRequiredService<IConfiguration>();
    }
}

/// <summary>
/// Simple in-memory event bus implementation
/// </summary>
internal class SimpleEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<T>(Func<T, Task> handler) where T : class
    {
        var eventType = typeof(T);
        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = new List<Delegate>();
        }
        _handlers[eventType].Add(handler);
    }

    public async Task PublishAsync<T>(T eventData) where T : class
    {
        var eventType = typeof(T);
        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            return;
        }

        foreach (var handler in handlers.Cast<Func<T, Task>>())
        {
            await handler(eventData);
        }
    }
}

/// <summary>
/// Exception thrown when plugin is not found
/// </summary>
public class PluginNotFoundException : Exception
{
    public string PluginName { get; }

    public PluginNotFoundException(string pluginName)
        : base($"Plugin not found: {pluginName}")
    {
        PluginName = pluginName;
    }
}
