using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Formitable.BetterStack.Logger;

public record BetterStackLogEnvelope(
    string Message,
    string Level,
    IDictionary<string, object> Metadata)
{
    [JsonPropertyName("dt")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
