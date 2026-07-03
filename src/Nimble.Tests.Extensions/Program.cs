using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nimble.Extensions.DependencyInjection.Extensions;
using Nimble.Extensions.Logging;
using Nimble.Tests.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging
    .SetMinimumLevel(LogLevel.Trace)
    .AddPrettierConsole(static configure =>
    {
        configure.TimestampColor = ConsoleColor.DarkCyan;
        configure.SpecialCategoryPrefix = "Nimble.Tests";
    })
    .AddConsoleListener(static configure =>
    {
        configure.CreateScopes = true;
        configure.OnReadlineCompleted = HandleCommand;
    });

builder.Services.AddLazyServices();
builder.Services.AddHostedService<TestSrv>();

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
