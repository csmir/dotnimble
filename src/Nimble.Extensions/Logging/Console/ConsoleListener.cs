using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Nimble.Extensions.Logging.Console;

internal sealed class ConsoleListener : BackgroundService, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly IDisposable? _optionsReloadToken;
    private readonly WrittenLogTracker? _logTracker;
    private ConsoleListenerOptions _options;

    public ConsoleListener(IOptionsMonitor<ConsoleListenerOptions> options, IServiceProvider services)
    {
        _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        _options = options.CurrentValue;
        _logTracker = services.GetService<WrittenLogTracker>();
        _services = services;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
    }

    private void ReloadLoggerOptions(ConsoleListenerOptions options)
        => _options = options;

    public override void Dispose()
        => _optionsReloadToken?.Dispose();
}
