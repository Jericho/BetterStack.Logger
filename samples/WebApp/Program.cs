using System;
using Formitable.BetterStack.Logger.Microsoft;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddLogging(logBuilder =>
{
    logBuilder.AddBetterStackLogger(conf =>
    {
        // You can also define this configuration as part of the logging configuration.
        // See: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/#configure-logging
        // An example is in the appsettings.json of this project.

        // If you prefer doing this by code, see below:
        //conf.SourceToken = builder.Configuration["BetterStack:SourceToken"];
        //conf.Endpoint = new Uri("https://self-hosted.local");

        conf.Context["ServerName"] = Environment.MachineName;
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseRouting();

app.MapRazorPages();

app.Run();
