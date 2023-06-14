using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Formitable.BetterStack.Logger.Microsoft;

[UnsupportedOSPlatform("browser")]
[ProviderAlias("BetterStack")]
internal sealed class BetterStackLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly Task _flushTask;
    private readonly IBetterStackLogClient _client;
    private readonly IDisposable? _onChangeToken;
    private readonly ConcurrentDictionary<string, BetterStackLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly BlockingCollection<BetterStackLogEnvelope> _logQueue = new(new ConcurrentQueue<BetterStackLogEnvelope>());
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private BetterStackLoggerConfiguration _currentConfig;

    internal IExternalScopeProvider? ScopeProvider { get; private set; }

    public BetterStackLoggerProvider(IOptionsMonitor<BetterStackLoggerConfiguration> config, IBetterStackLogClient client)
    {
        _client = client;
        _currentConfig = config.CurrentValue;
        _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
        _flushTask = Task.Run(FlushQueue);
    }

    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, name => new BetterStackLogger(name, this));

    internal void EnqueueLogEvent(BetterStackLogEnvelope logEnvelope)
    {
        try
        {
            if (!_logQueue.TryAdd(logEnvelope))
            {
                // TODO: Record failure?
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to queue logs to BetterStack: {ex.Message}");
        }
    }

    private async Task FlushQueue()
    {
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            var limit = _currentConfig.BatchSize;
            var batch = new List<BetterStackLogEnvelope>();

            while (limit > 0 && _logQueue.TryTake(out var message))
            {
                batch.Add(message);
                limit--;
            }

            if (batch.Any())
            {
                try
                {
                    await _client.UploadAsync(batch, _cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to upload logs to BetterStack: {ex.Message}");
                }
            }
            else
            {
                await Task.Delay(_currentConfig.FlushFrequency, _cancellationTokenSource.Token);
            }
        }
    }

    public void Dispose()
    {
        _loggers.Clear();
        _onChangeToken?.Dispose();
        _cancellationTokenSource.Dispose();

        try
        {
            _flushTask.Wait(_currentConfig.FlushFrequency);
        }
        catch (TaskCanceledException)
        {
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TaskCanceledException)
        {
        }
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => ScopeProvider = scopeProvider;
}
