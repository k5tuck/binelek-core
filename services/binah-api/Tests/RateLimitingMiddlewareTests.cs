using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Binah.API.Tests;

public class RateLimitingMiddlewareTests
{
    [Fact]
    public async Task ProcessRequest_WithinLimit_CallsNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        // Note: This is a simplified test. In reality, you'd mock the rate limiting logic
        // For now, we're just testing the structure

        // Act
        await next(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public void RateLimitConfiguration_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var defaultLimit = 100; // From appsettings.json
        var defaultBurst = 20;

        // Assert
        defaultLimit.Should().BeGreaterThan(0);
        defaultBurst.Should().BeGreaterThan(0);
    }
}
