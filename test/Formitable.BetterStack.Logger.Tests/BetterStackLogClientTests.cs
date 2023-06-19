using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using RichardSzalay.MockHttp;
using Xunit;

namespace Formitable.BetterStack.Logger.Tests;

public class BetterStackLogClientTests
{
    [Fact]
    public async Task GivenLogs_UploadsToGivenEndpoint()
    {
        // Arrange
        var expectedEndpoint = "https://test.dev.local";
        var expectedSourceToken = Guid.NewGuid().ToString();

        var expectedLog = new BetterStackLogEnvelope("Test log", "Information", new Dictionary<string, object>
        {
            { "TestMetadata", "Test value" },
        });

        string? requestBody = null;
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Post, $"{expectedEndpoint}/")
            .WithHeaders("Authorization", $"Bearer {expectedSourceToken}")
            .Respond(HttpStatusCode.Accepted, (req) =>
            {
                requestBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                return new StringContent(string.Empty);
            });

        var client = new BetterStackLogClient(new BetterStackLogConfiguration
        {
            Endpoint = new Uri(expectedEndpoint),
            SourceToken = expectedSourceToken,
        }, mockHttp.ToHttpClient());

        // Act
        await client.UploadAsync(new[]
        {
            expectedLog,
        });

        // Assert
        mockHttp.VerifyNoOutstandingExpectation();
        Assert.Equal($"[{{\"message\":\"Test log\",\"level\":\"Information\",\"metadata\":{{\"TestMetadata\":\"Test value\"}},\"dt\":\"{expectedLog.Timestamp:yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz}\"}}]", requestBody);
    }

    [Fact]
    public async Task GivenLogs_WithContext_UploadsEnrichedLogsToGivenEndpoint()
    {
        // Arrange
        var expectedEndpoint = "https://test.dev.local";
        var expectedSourceToken = Guid.NewGuid().ToString();

        var expectedLog = new BetterStackLogEnvelope("Test log", "Information", new Dictionary<string, object>());

        string? requestBody = null;
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Post, $"{expectedEndpoint}/")
            .WithHeaders("Authorization", $"Bearer {expectedSourceToken}")
            .Respond(HttpStatusCode.Accepted, (req) =>
            {
                requestBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                return new StringContent(string.Empty);
            });

        var client = new BetterStackLogClient(new BetterStackLogConfiguration
        {
            Endpoint = new Uri(expectedEndpoint),
            SourceToken = expectedSourceToken,
            Context = new Dictionary<string, object>
            {
                { "Test context", "Test value" }
            }
        }, mockHttp.ToHttpClient());

        // Act
        await client.UploadAsync(new[]
        {
            expectedLog,
        });

        // Assert
        mockHttp.VerifyNoOutstandingExpectation();
        Assert.Equal($"[{{\"message\":\"Test log\",\"level\":\"Information\",\"metadata\":{{\"Test context\":\"Test value\"}},\"dt\":\"{expectedLog.Timestamp:yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz}\"}}]", requestBody);
    }

    [Fact]
    public async Task GivenLogs_WithException_UploadsFormattedException()
    {
        // Arrange
        var expectedEndpoint = "https://test.dev.local";
        var expectedSourceToken = Guid.NewGuid().ToString();

        var expectedLog = new BetterStackLogEnvelope("Test log", "Information", new Dictionary<string, object>());

        try
        {
            throw new Exception("Test exception");
        }
        catch (Exception ex)
        {
            expectedLog.Metadata["Exception"] = ex;
        }

        string? requestBody = null;
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Post, $"{expectedEndpoint}/")
            .WithHeaders("Authorization", $"Bearer {expectedSourceToken}")
            .Respond(HttpStatusCode.Accepted, (req) =>
            {
                requestBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                return new StringContent(string.Empty);
            });

        var client = new BetterStackLogClient(new BetterStackLogConfiguration
        {
            Endpoint = new Uri(expectedEndpoint),
            SourceToken = expectedSourceToken,
        }, mockHttp.ToHttpClient());

        // Act
        await client.UploadAsync(new[]
        {
            expectedLog,
        });

        // Assert
        mockHttp.VerifyNoOutstandingExpectation();
        Assert.Equal($"[{{\"message\":\"Test log\",\"level\":\"Information\",\"metadata\":{{\"Exception\":{{\"message\":\"Test exception\",\"data\":{{}},\"innerException\":null,\"helpLink\":null,\"source\":\"Formitable.BetterStack.Logger.Tests\",\"hResult\":-2146233088,\"stackTrace\":\"{(expectedLog.Metadata["Exception"] as Exception)?.StackTrace?.Replace("\\", "\\\\")}\"}}}},\"dt\":\"{expectedLog.Timestamp:yyyy-MM-ddTHH:mm:ss.FFFFFFFzzz}\"}}]", requestBody);
    }
}
