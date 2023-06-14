using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace Formitable.BetterStack.Logger.Microsoft;

public static class BetterStackLoggerExtensions
{
    /// <summary>
    /// Adds logging for BetterStack Logs to the logging chain.
    /// </summary>
    public static ILoggingBuilder AddBetterStackLogger(
        this ILoggingBuilder builder)
    {
        builder.AddConfiguration();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, BetterStackLoggerProvider>());

        LoggerProviderOptions.RegisterProviderOptions<BetterStackLoggerConfiguration, BetterStackLoggerProvider>(builder.Services);

        static BetterStackLogClient CreateClient(IServiceProvider services)
        {
            var config = services.GetRequiredService<IOptions<BetterStackLoggerConfiguration>>().Value;
            return new BetterStackLogClient(config);
        }

        builder.Services.AddTransient<IBetterStackLogClient, BetterStackLogClient>((sp) => CreateClient(sp));

        return builder;
    }

    /// <summary>
    /// Adds logging for BetterStack Logs to the logging chain.
    /// </summary>
    public static ILoggingBuilder AddBetterStackLogger(
        this ILoggingBuilder builder,
        Action<BetterStackLoggerConfiguration> configure)
    {
        builder.AddBetterStackLogger();
        builder.Services.Configure(configure);

        return builder;
    }
}
