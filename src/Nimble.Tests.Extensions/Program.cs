using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nimble.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging
    .SetMinimumLevel(LogLevel.Trace)
    .AddPrettierConsole(configure =>
    {
        configure.MaxLogWidth = 120;
        configure.TimestampColor = ConsoleColor.DarkCyan;
        configure.SpecialCategoryPrefix = "Nimble";
    })
    .AddConsoleListener(configure =>
    {
        configure.CreateScopes = true;
        configure.OnReadlineCompleted = HandleCommand;
    });

var app = builder.Build();

app.Run();

static void HandleCommand(string input, IServiceProvider srv, CancellationToken cancellation)
{
    var logger = srv.GetRequiredService<ILogger<Program>>();

    if (input.Equals("t", StringComparison.OrdinalIgnoreCase))
    {
        logger.LogInformation("You entered the test command.");
        logger.LogInformation("You entered the test command.");
        logger.LogInformation("You entered the test command.");
        logger.LogInformation("You entered the test command.");
        logger.LogInformation("You entered the test command.");
    }
    else
        logger.LogWarning("Unrecognized command: {Input}", input);
}