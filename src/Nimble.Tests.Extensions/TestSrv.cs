using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nimble.Extensions.DependencyInjection;
using Nimble.Extensions.Logging.Console;

namespace Nimble.Tests.Extensions;

internal class TestSrv(ILogger<TestSrv> logger, IServiceLazy<WrittenLogTracker> tracker) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            throw new ArgumentException("This is a test exception to demonstrate logging.", new InvalidOperationException("This is the inner exception."));
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "An unhandled exception occurred during application startup.");
        }

        var x = tracker.Value;

        logger.LogInformation("Log tracker active, tracking status: {X}", x.LogIsTracked());

        return Task.CompletedTask;
    }
}
