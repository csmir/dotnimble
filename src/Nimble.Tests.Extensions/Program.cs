using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nimble.Extensions.Logging;
using Nimble.Extensions.Logging.Console;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Logging.AddPrettierConsole(configure =>
{
    configure.MaxLogWidth = 120;
    configure.TimestampColor = ConsoleColor.DarkCyan;
    configure.SpecialCategoryPrefix = "Nimble";
});

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<PrettierFormatter>>();

using var logScope = logger.BeginScope("Centralized program log.");

logger.LogDebug("Test 1");
logger.LogDebug("Test 2");

app.Run();