using System;
using System.Collections.Generic;

namespace Formitable.BetterStack.Logger;

public class BetterStackLogConfiguration
{
    /// <summary>
    /// The source token to use for authenticating logs being uploaded.
    /// </summary>
    public string? SourceToken { get; set; }

    /// <summary>
    /// The endpoint to send logs to, defaults to the official `https://in.logs.betterstack.com`.
    /// </summary>
    public Uri Endpoint { get; set; } = new("https://in.logs.betterstack.com/");

    /// <summary>
    /// Additional metadata to be added to all logs, useful for capturing environment details.
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
}
