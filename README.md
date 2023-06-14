# .Net package for BetterStack logging

This repo contains packages for easier logging to BetterStack for C#.

Currently you will find:

* `Formitable.BetterStack.Logger`: contains a client to serialize and upload logs
* `Formitable.BetterStack.Logger.Microsoft`: contains extensions to add the client to the Microsoft `ILogger` pipeline

## Usage through ILogger<>

Install the package from NuGet:

```bash
dotnet add package Formitable.BetterStack.Logger.Microsoft
```

Then add to your logging pipeline:

```csharp
builder.Services.AddLogging(logBuilder =>
{
    logBuilder.AddBetterStackLogger(conf =>
    {
        conf.SourceToken = builder.Configuration["BetterStack:SourceToken"];
    });
});
```

A sample implementation can be found in the `/samples` directory.

At the very least, you need to specify a source token, and this can be done in code or in configuration.
To enrich all logs being uploaded with relevant context, add data to the `Context` dictionary of the configuration.

By default, the logger is configured to batch and upload batches every second, in batches of 1000. If you require higher throughput, this can be adjusted in the configuration.
