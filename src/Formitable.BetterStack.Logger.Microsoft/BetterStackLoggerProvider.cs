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

    // This task runs continously until the cancellation token is canceled
    private async Task FlushQueue()
    {
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            int messagesFlushed = await FlushBatch().ConfigureAwait(false);
            if (messagesFlushed == 0)
            {
                await Task.Delay(_currentConfig.FlushFrequency, _cancellationTokenSource.Token).ConfigureAwait(false);
            }
        }
    }

    // This method uploads a batch of messages to betterstack.com
    private async Task<int> FlushBatch()
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
                // Cancellation.None on the following line might seem counterintuitive, but it's
                // actually quite important. We want to ensure the upload of a given batch of
                // messages completes sucessfully even when _cancellationTokenSource.Token is
                // cancelled (which indicates that BetterStackLoggerProvider is being disposed).
                await _client.UploadAsync(batch, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to upload logs to BetterStack: {ex.Message}");
            }
        }

        return batch.Count;
    }

    public void Dispose()
    {
        _loggers.Clear();
        _onChangeToken?.Dispose();

        // Stop "_flushTask" and wait for it completes
        _cancellationTokenSource.Cancel();
        _flushTask.Wait(TimeSpan.FromMilliseconds(500));

        // There's a possibility the Task was stoped before the queue
        // could be fully processed. Therefore we need to flush any
        // remaining messages.
        try
        {
            // Flush remaining messages until the queue is empty
            int batchCount = 0;
            do
            {
                batchCount = FlushBatch().GetAwaiter().GetResult();
            } while (batchCount > 0);
        }
        catch (TaskCanceledException)
        {
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TaskCanceledException)
        {
        }
        finally
        {
            _cancellationTokenSource.Dispose();
        }
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => ScopeProvider = scopeProvider;
}
