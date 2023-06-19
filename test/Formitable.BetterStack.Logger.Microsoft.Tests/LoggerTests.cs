using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Formitable.BetterStack.Logger.Microsoft.Tests;

public class LoggerTests
{
    private readonly Mock<IBetterStackLogClient> _clientMock;
    private readonly ILogger<LoggerTests> _logger;

    public LoggerTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddBetterStackLogger(conf =>
            {
                conf.FlushFrequency = TimeSpan.FromMilliseconds(50);
            });
        });

        _clientMock = new Mock<IBetterStackLogClient>(MockBehavior.Strict);
        services.AddSingleton(_clientMock.Object);

        var provider = services.BuildServiceProvider();
        _logger = provider.GetRequiredService<ILogger<LoggerTests>>();
    }

    [Fact]
    public async Task OnLog_UploadsLogs()
    {
        var shippedLogs = new List<BetterStackLogEnvelope>();
        _clientMock.Setup(c => c.UploadAsync(It.IsAny<IEnumerable<BetterStackLogEnvelope>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<BetterStackLogEnvelope>, CancellationToken>((logs, ct) => shippedLogs.AddRange(logs))
            .Returns(Task.CompletedTask);

        // Act
        _logger.LogDebug("Test debug");
        _logger.LogInformation("Test info");
        _logger.LogWarning("Test warning");
        _logger.LogError("Test error");
        _logger.LogCritical("Test critical");
        await Task.Delay(100);

        // Assert
        Assert.Collection(shippedLogs,
            log =>
            {
                Assert.Equal("Debug", log.Level);
                Assert.Equal("Test debug", log.Message);
            },
            log =>
            {
                Assert.Equal("Information", log.Level);
                Assert.Equal("Test info", log.Message);
            },
            log =>
            {
                Assert.Equal("Warning", log.Level);
                Assert.Equal("Test warning", log.Message);
            },
            log =>
            {
                Assert.Equal("Error", log.Level);
                Assert.Equal("Test error", log.Message);
            },
            log =>
            {
                Assert.Equal("Critical", log.Level);
                Assert.Equal("Test critical", log.Message);
            });
    }

    [Fact]
    public async Task OnLog_WithException_ForwardsException()
    {
        var shippedLogs = new List<BetterStackLogEnvelope>();
        _clientMock.Setup(c => c.UploadAsync(It.IsAny<IEnumerable<BetterStackLogEnvelope>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<BetterStackLogEnvelope>, CancellationToken>((logs, ct) => shippedLogs.AddRange(logs))
            .Returns(Task.CompletedTask);

        // Act
        try
        {
            throw new Exception("Test exception");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test error");
        }
        await Task.Delay(100);

        // Assert
        Assert.Collection(shippedLogs,
            log =>
            {
                Assert.Equal("Error", log.Level);
                Assert.Equal("Test error", log.Message);
                var exception = Assert.IsType<Exception>(log.Metadata["Exception"]);
                Assert.Equal("Test exception", exception.Message);
            });
    }

    [Fact]
    public async Task OnLog_WithStructuredContent_ForwardsMetadata()
    {
        var shippedLogs = new List<BetterStackLogEnvelope>();
        _clientMock.Setup(c => c.UploadAsync(It.IsAny<IEnumerable<BetterStackLogEnvelope>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<BetterStackLogEnvelope>, CancellationToken>((logs, ct) => shippedLogs.AddRange(logs))
            .Returns(Task.CompletedTask);

        // Act
        _logger.LogError("Test error with {TestData}", "Test value");
        await Task.Delay(100);

        // Assert
        Assert.Collection(shippedLogs,
            log =>
            {
                Assert.Equal("Error", log.Level);
                Assert.Equal("Test error with Test value", log.Message);
                var data = Assert.IsType<string>(log.Metadata["TestData"]);
                Assert.Equal("Test value", data);
            });
    }

    [Fact]
    public async Task OnLog_InScopes_ForwardsScopePath()
    {
        var shippedLogs = new List<BetterStackLogEnvelope>();
        _clientMock.Setup(c => c.UploadAsync(It.IsAny<IEnumerable<BetterStackLogEnvelope>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<BetterStackLogEnvelope>, CancellationToken>((logs, ct) => shippedLogs.AddRange(logs))
            .Returns(Task.CompletedTask);

        // Act
        using (_logger.BeginScope("Outer scope"))
        {
            using (_logger.BeginScope("Inner scope"))
            {
                _logger.LogInformation("Test message");
            }
        }
        await Task.Delay(100);

        // Assert
        Assert.Collection(shippedLogs,
            log =>
            {
                Assert.Equal("Information", log.Level);
                Assert.Equal("Test message", log.Message);
                var scope = Assert.IsType<string>(log.Metadata["Scope"]);
                Assert.Equal("Outer scope / Inner scope", scope);
            });
    }
}
