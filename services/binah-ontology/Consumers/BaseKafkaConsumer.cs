using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Binah.Ontology.Models.Exceptions;

namespace Binah.Ontology.Consumers;

/// <summary>
/// Abstract base class for all Kafka consumers in binah-ontology service
/// Implements BackgroundService pattern with manual offset commits
/// </summary>
/// <typeparam name="TEvent">The event type to consume</typeparam>
public abstract class BaseKafkaConsumer<TEvent> : BackgroundService where TEvent : class
{
    private readonly IConsumer<string, string> _consumer;
    protected readonly ILogger Logger;
    private readonly string _topic;
    private readonly int _maxRetries;
    private readonly int _retryDelayMs;

    protected BaseKafkaConsumer(
        IConfiguration configuration,
        ILogger logger,
        string topic,
        string consumerGroupId)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
        if (logger == null)
            throw new ArgumentNullException(nameof(logger));
        if (string.IsNullOrWhiteSpace(topic))
            throw new ArgumentException("Topic cannot be null or empty", nameof(topic));
        if (string.IsNullOrWhiteSpace(consumerGroupId))
            throw new ArgumentException("Consumer group ID cannot be null or empty", nameof(consumerGroupId));

        Logger = logger;
        _topic = topic;
        _maxRetries = configuration.GetValue<int>("Kafka:Consumer:MaxRetries", 3);
        _retryDelayMs = configuration.GetValue<int>("Kafka:Consumer:RetryDelayMs", 1000);

        var bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? throw new OntologyConfigurationException("Kafka:BootstrapServers configuration is missing");

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = consumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false, // Manual commit for better control
            EnableAutoOffsetStore = false,
            MaxPollIntervalMs = 300000, // 5 minutes
            SessionTimeoutMs = 45000,
            HeartbeatIntervalMs = 3000,
            AllowAutoCreateTopics = false
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((_, e) =>
            {
                Logger.LogError("Kafka consumer error: Code={Code}, Reason={Reason}, IsFatal={IsFatal}",
                    e.Code, e.Reason, e.IsFatal);
            })
            .SetPartitionsAssignedHandler((_, partitions) =>
            {
                Logger.LogInformation("Partitions assigned: {Partitions}",
                    string.Join(", ", partitions));
            })
            .SetPartitionsRevokedHandler((_, partitions) =>
            {
                Logger.LogInformation("Partitions revoked: {Partitions}",
                    string.Join(", ", partitions));
            })
            .Build();

        Logger.LogInformation(
            "Kafka consumer initialized for topic {Topic} with group ID {GroupId}",
            topic, consumerGroupId);
    }

    /// <summary>
    /// Background service execution method
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("Starting Kafka consumer for topic: {Topic}", _topic);

        try
        {
            _consumer.Subscribe(_topic);
            Logger.LogInformation("Subscribed to topic: {Topic}", _topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Use timeout-based consume to avoid blocking the thread pool
                    // This allows the HTTP server to process requests concurrently
                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));

                    if (consumeResult?.Message == null)
                    {
                        // Yield control periodically when no messages are available
                        await Task.Delay(100, stoppingToken);
                        continue;
                    }

                    Logger.LogDebug(
                        "Received message from topic {Topic}, partition {Partition}, offset {Offset}",
                        consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);

                    // Deserialize event
                    TEvent? @event = null;
                    try
                    {
                        @event = JsonSerializer.Deserialize<TEvent>(
                            consumeResult.Message.Value,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        if (@event == null)
                        {
                            Logger.LogWarning(
                                "Failed to deserialize message from offset {Offset}: null result",
                                consumeResult.Offset);
                            _consumer.Commit(consumeResult);
                            continue;
                        }
                    }
                    catch (JsonException ex)
                    {
                        Logger.LogError(ex,
                            "Failed to deserialize message from offset {Offset}. Skipping message.",
                            consumeResult.Offset);
                        _consumer.Commit(consumeResult); // Commit to skip malformed message
                        continue;
                    }

                    // Process event with retry logic
                    var success = await ProcessEventWithRetryAsync(@event, stoppingToken);

                    if (success)
                    {
                        // Commit offset after successful processing
                        try
                        {
                            _consumer.Commit(consumeResult);
                            Logger.LogDebug("Committed offset {Offset}", consumeResult.Offset);
                        }
                        catch (KafkaException ex)
                        {
                            Logger.LogError(ex, "Failed to commit offset {Offset}", consumeResult.Offset);
                            // Continue processing - offset will be reprocessed on restart
                        }
                    }
                    else
                    {
                        Logger.LogError(
                            "Failed to process message after {MaxRetries} retries. Moving to next message.",
                            _maxRetries);
                        // Still commit to avoid infinite reprocessing
                        // In production, should send to dead-letter queue
                        _consumer.Commit(consumeResult);
                    }
                }
                catch (ConsumeException ex)
                {
                    Logger.LogError(ex, "Error consuming message from Kafka: {Error}", ex.Error.Reason);
                    await Task.Delay(1000, stoppingToken); // Brief delay before retry
                }
                catch (OperationCanceledException)
                {
                    Logger.LogInformation("Consumer operation cancelled (shutdown in progress)");
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Unexpected error in consumer loop");
                    await Task.Delay(5000, stoppingToken); // Delay before retry
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("Consumer stopped due to cancellation");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Fatal error in Kafka consumer");
            throw;
        }
        finally
        {
            Logger.LogInformation("Closing Kafka consumer for topic: {Topic}", _topic);
            try
            {
                _consumer.Close();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error closing Kafka consumer");
            }
        }
    }

    /// <summary>
    /// Processes an event with exponential backoff retry logic
    /// </summary>
    private async Task<bool> ProcessEventWithRetryAsync(TEvent @event, CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                await ProcessEventAsync(@event, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex,
                    "Failed to process event (attempt {Attempt}/{MaxRetries}): {Message}",
                    attempt, _maxRetries, ex.Message);

                if (attempt < _maxRetries)
                {
                    // Exponential backoff: 1s, 2s, 4s, etc.
                    var delayMs = _retryDelayMs * (int)Math.Pow(2, attempt - 1);
                    Logger.LogDebug("Retrying in {DelayMs}ms...", delayMs);
                    await Task.Delay(delayMs, cancellationToken);
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Abstract method to be implemented by derived classes for event processing
    /// </summary>
    /// <param name="event">The event to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected abstract Task ProcessEventAsync(TEvent @event, CancellationToken cancellationToken);

    /// <summary>
    /// Graceful shutdown
    /// </summary>
    public override void Dispose()
    {
        try
        {
            Logger.LogInformation("Disposing Kafka consumer for topic: {Topic}", _topic);
            _consumer?.Close();
            _consumer?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error disposing Kafka consumer");
        }
        finally
        {
            base.Dispose();
        }
    }
}
