using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Formitable.BetterStack.Logger;

public interface IBetterStackLogClient
{
    Task UploadAsync(IEnumerable<BetterStackLogEnvelope> logs, CancellationToken cancellationToken = default);
}

public sealed class BetterStackLogClient : IBetterStackLogClient, IDisposable
{
    private readonly AsyncRetryPolicy<HttpResponseMessage> _policy;
    private readonly Dictionary<string, object> _context;
    private readonly HttpClient _httpClient;
    private readonly bool _httpClientIsOwned;

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter(),
        },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public BetterStackLogClient(BetterStackLogConfiguration configuration, HttpClient? httpClient = null)
    {
        _context = configuration.Context;

        if (configuration.Endpoint == null)
        {
            throw new ArgumentNullException(nameof(configuration.Endpoint));
        }

        if (configuration.SourceToken == null)
        {
            throw new ArgumentNullException(nameof(configuration.SourceToken));
        }

        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = configuration.Endpoint;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configuration.SourceToken);
        _httpClientIsOwned = httpClient == null;
        _policy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task UploadAsync(IEnumerable<BetterStackLogEnvelope> logs, CancellationToken cancellationToken = default)
    {
        // Enrich each log event with client specific context
        foreach (var log in logs)
        {
            foreach (var kv in _context)
            {
                log.Metadata[kv.Key] = kv.Value;
            }

            try
            {
                JsonSerializer.Serialize(log, _serializerOptions);
            }
            catch
            {
                var metadataValues = log.Metadata.Values.Select(v => (v?.GetType().Name ?? "null!", v)).ToArray();
            }
        }

        var json = JsonSerializer.Serialize(logs, _serializerOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _policy.ExecuteAndCaptureAsync((ct) => _httpClient.PostAsync("/", content, ct), cancellationToken);
        response.Result.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        if (_httpClientIsOwned)
        {
            _httpClient.Dispose();
        }
    }
}
