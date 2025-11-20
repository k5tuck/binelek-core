namespace Binah.Ontology.Models.Exceptions;

/// <summary>
/// Exception thrown when Kafka event publishing fails
/// </summary>
public class EventPublishException : OntologyException
{
    public string EventType { get; set; }
    public string Topic { get; set; }

    public EventPublishException(string eventType, string topic, string message)
        : base($"Failed to publish event '{eventType}' to topic '{topic}': {message}",
               "EVENT_PUBLISH_FAILED")
    {
        EventType = eventType;
        Topic = topic;
    }

    public EventPublishException(string eventType, string topic, string message, System.Exception innerException)
        : base($"Failed to publish event '{eventType}' to topic '{topic}': {message}", innerException)
    {
        EventType = eventType;
        Topic = topic;
        ErrorCode = "EVENT_PUBLISH_FAILED";
    }
}