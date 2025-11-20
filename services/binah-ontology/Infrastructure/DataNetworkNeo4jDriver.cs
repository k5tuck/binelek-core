using Neo4j.Driver;

namespace Binah.Ontology.Infrastructure;

/// <summary>
/// Wrapper for the Data Network Neo4j driver to distinguish it from the production driver
/// Enables dependency injection with two separate Neo4j instances
/// </summary>
public interface IDataNetworkNeo4jDriver
{
    /// <summary>
    /// Get the Neo4j driver for the data network instance
    /// </summary>
    IDriver Driver { get; }
}

/// <summary>
/// Implementation of Data Network Neo4j driver wrapper
/// </summary>
public class DataNetworkNeo4jDriver : IDataNetworkNeo4jDriver
{
    public IDriver Driver { get; }

    public DataNetworkNeo4jDriver(IDriver driver)
    {
        Driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }
}
