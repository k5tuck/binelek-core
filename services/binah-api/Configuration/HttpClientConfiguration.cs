using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace Binah.API.Configuration;

/// <summary>
/// Configuration for HTTP clients with resilience policies
/// </summary>
public static class HttpClientConfiguration
{
    public static IServiceCollection AddBinahHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var ontologyUrl = configuration["Services:Ontology:Url"]
            ?? throw new InvalidOperationException("Ontology service URL is not configured");
        var contextUrl = configuration["Services:Context:Url"]
            ?? throw new InvalidOperationException("Context service URL is not configured");
        var authUrl = configuration["Services:Auth:Url"]
            ?? throw new InvalidOperationException("Auth service URL is not configured");

        // Ontology Service Client
        services.AddHttpClient("OntologyService", client =>
        {
            client.BaseAddress = new Uri(ontologyUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(
                configuration.GetValue<int>("Services:Ontology:Timeout", 30));
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Context Service Client
        services.AddHttpClient("ContextService", client =>
        {
            client.BaseAddress = new Uri(contextUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(
                configuration.GetValue<int>("Services:Context:Timeout", 30));
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Auth Service Client
        services.AddHttpClient("AuthService", client =>
        {
            client.BaseAddress = new Uri(authUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(
                configuration.GetValue<int>("Services:Auth:Timeout", 30));
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // AI Service Client (Optional - if implemented)
        var aiUrl = configuration["Services:AI:Url"];
        if (!string.IsNullOrEmpty(aiUrl))
        {
            services.AddHttpClient("AIService", client =>
            {
                client.BaseAddress = new Uri(aiUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(
                    configuration.GetValue<int>("Services:AI:Timeout", 60));
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
        }

        // LLM Service Client (Optional - if implemented)
        var llmUrl = configuration["Services:LLM:Url"];
        if (!string.IsNullOrEmpty(llmUrl))
        {
            services.AddHttpClient("LLMService", client =>
            {
                client.BaseAddress = new Uri(llmUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(
                    configuration.GetValue<int>("Services:LLM:Timeout", 120));
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
        }

        return services;
    }

    /// <summary>
    /// Retry policy for transient HTTP errors
    /// Retries 3 times with exponential backoff
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Log retry attempts if needed
                    Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s due to: {outcome.Result?.StatusCode}");
                });
    }

    /// <summary>
    /// Circuit breaker policy to prevent cascading failures
    /// Opens circuit after 5 consecutive failures for 30 seconds
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s due to: {outcome.Result?.StatusCode}");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset");
                });
    }
}
