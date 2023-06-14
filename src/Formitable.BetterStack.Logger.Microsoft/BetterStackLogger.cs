using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Formitable.BetterStack.Logger.Microsoft;

internal sealed class BetterStackLogger : ILogger
{
    private readonly string _name;
    private readonly BetterStackLoggerProvider _provider;

    public BetterStackLogger(string name, BetterStackLoggerProvider provider) => (_name, _provider) = (name, provider);

    public IDisposable BeginScope<TState>(TState state) => default!;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        var metadata = new Dictionary<string, object>
        {
            { "Logger", _name },
        };

        var scopeProvider = _provider.ScopeProvider;
        if (scopeProvider != null)
        {
            var scopes = new List<string?>();
            scopeProvider.ForEachScope((scope, list) => list.Add(scope?.ToString()), scopes);
            if (scopes.Any())
            {
                metadata["Scope"] = string.Join(" / ", scopes.OfType<string>());
            }
        }

        if (exception != null)
        {
            metadata["Exception"] = exception;
        }

        if (state is IEnumerable<KeyValuePair<string, object>> structuredLogData)
        {
            foreach (var kv in structuredLogData)
            {
                if (kv.Key == null || kv.Value == null) continue;
                if (kv.Value.GetType().Namespace?.StartsWith(nameof(Microsoft)) ?? false) continue; // A lot of Microsoft types are not serializable
                metadata[kv.Key] = kv.Value;
            }
        }

        var envelope = new BetterStackLogEnvelope(message, logLevel.ToString(), metadata);
        _provider.EnqueueLogEvent(envelope);
    }
}
