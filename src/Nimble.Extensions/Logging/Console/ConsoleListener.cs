using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nimble.Extensions.Logging.Console;

internal sealed class ConsoleListener : BackgroundService, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly IDisposable? _optionsReloadToken;
    private readonly ILogger<ConsoleListener> _logger;
    private readonly WrittenLogTracker? _logTracker;
    private ConsoleListenerOptions _options;

    public ConsoleListener(IOptionsMonitor<ConsoleListenerOptions> options, WrittenLogTracker? logTracker, ILogger<ConsoleListener> logger, IServiceProvider services)
    {
        _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        _options = options.CurrentValue;
        _logTracker = logTracker;
        _services = services;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Console listener started. Waiting for user input...");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_options.OnReadlineCompleted != null)
            {
                var output = System.Console.ReadLine();

                if (output == null)
                    break;

                _logTracker?.Reset();

                var services = _options.CreateScopes 
                    ? _services.CreateScope().ServiceProvider 
                    : _services;

                _options.OnReadlineCompleted(output, services, stoppingToken);
            }
        }

        _logger.LogInformation("Console listener stopped.");
    }

    private void ReloadLoggerOptions(ConsoleListenerOptions options)
        => _options = options;

    public override void Dispose()
        => _optionsReloadToken?.Dispose();
}
