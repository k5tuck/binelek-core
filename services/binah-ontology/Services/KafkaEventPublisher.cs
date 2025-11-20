using Binah.Ontology.Models;
using Binah.Ontology.Models.Exceptions;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Binah.Ontology.Services;

/// <summary>
/// Kafka-based implementation of event publisher
/// </summary>
public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private readonly string _ontologyTopic;

    public KafkaEventPublisher(
        IProducer<string, string> producer,
        IConfiguration configuration,
        ILogger<KafkaEventPublisher> logger)
    {
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _ontologyTopic = configuration["Kafka:Topics:OntologyEvents"]
            ?? "Binah.ontology.events";
    }

    /// <inheritdoc/>
    public async Task<bool> PublishEntityCreatedAsync(EntityCreatedEvent @event)
    {
        return await PublishEventAsync(@event, $"entity.created.{@event.EntityId}");
    }

    /// <inheritdoc/>
    public async Task<bool> PublishEntityUpdatedAsync(EntityUpdatedEvent @event)
    {
        return await PublishEventAsync(@event, $"entity.updated.{@event.EntityId}");
    }

    /// <inheritdoc/>
    public async Task<bool> PublishEntityDeletedAsync(EntityDeletedEvent @event)
    {
        return await PublishEventAsync(@event, $"entity.deleted.{@event.EntityId}");
    }

    /// <inheritdoc/>
    public async Task<bool> PublishRelationshipCreatedAsync(RelationshipCreatedEvent @event)
    {
        return await PublishEventAsync(@event, $"relationship.created.{@event.FromEntityId}");
    }

    /// <inheritdoc/>
    public async Task<bool> PublishRelationshipDeletedAsync(RelationshipDeletedEvent @event)
    {
        return await PublishEventAsync(@event, $"relationship.deleted.{@event.FromEntityId}");
    }

    private async Task<bool> PublishEventAsync<T>(T @event, string key) where T : OntologyEvent
    {
        try
        {
            _logger.LogDebug("Publishing event {EventType} with key {Key}", @event.EventType, key);

            var eventJson = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var message = new Message<string, string>
            {
                Key = key,
                Value = eventJson,
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(@event.EventType) },
                    { "event-id", System.Text.Encoding.UTF8.GetBytes(@event.EventId) },
                    { "timestamp", System.Text.Encoding.UTF8.GetBytes(@event.Timestamp.ToString("O")) }
                }
            };

            var deliveryResult = await _producer.ProduceAsync(_ontologyTopic, message);

            if (deliveryResult.Status == PersistenceStatus.Persisted)
            {
                _logger.LogInformation(
                    "Event {EventType} published successfully. Partition: {Partition}, Offset: {Offset}",
                    @event.EventType,
                    deliveryResult.Partition,
                    deliveryResult.Offset);

                return true;
            }
            else
            {
                _logger.LogWarning(
                    "Event {EventType} published but not persisted. Status: {Status}",
                    @event.EventType,
                    deliveryResult.Status);

                return false;
            }
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex,
                "Failed to publish event {EventType}. Error: {Error}",
                @event.EventType,
                ex.Error.Reason);

            throw new EventPublishException(
                @event.EventType,
                _ontologyTopic,
                $"Kafka produce error: {ex.Error.Reason}",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error publishing event {EventType}", @event.EventType);

            throw new EventPublishException(
                @event.EventType,
                _ontologyTopic,
                "Unexpected error during event publishing",
                ex);
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}
