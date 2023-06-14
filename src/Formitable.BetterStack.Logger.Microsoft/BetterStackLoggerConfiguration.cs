using System;

namespace Formitable.BetterStack.Logger.Microsoft;

public sealed class BetterStackLoggerConfiguration : BetterStackLogConfiguration
{
    /// <summary>
    /// How often the queue of logs should be uploaded to BetterStack.
    /// </summary>
    public TimeSpan FlushFrequency { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The size of each batch sent to BetterStack.
    /// </summary>
    public int BatchSize { get; set; } = 1_000;
}
