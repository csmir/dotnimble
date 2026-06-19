using System.ComponentModel.DataAnnotations;

namespace Nimble.Extensions.Logging.Console;

public sealed class ConsoleListenerOptions
{
    /// <summary>
    ///     Gets or sets an action that is invoked when a readline operation is completed in the console. This can be used to perform custom logic or trigger events after a user input is received.
    /// </summary>
    /// <remarks>
    ///     This value is required and must be set for the console listener to function properly. 
    ///     If not set, the console listener will not be able to process user input and may throw an exception or fail silently depending on the implementation of the console listener.
    /// </remarks>
    [Required]
    public required Action<string, IServiceProvider, CancellationToken> OnReadlineCompleted { get; set; }

    /// <summary>
    ///     Gets or sets whether the services provided to the <see cref="OnReadlineCompleted"/> action should be scoped. If set to true, a new scope will be created for each invocation of the action, allowing for scoped services to be resolved and used within the action.
    /// </summary>
    /// <remarks>
    ///     Default: false. If set to true, ensure that the action is designed to handle scoped services appropriately and that any necessary cleanup or disposal of scoped services is performed after the action completes.
    /// </remarks>
    public bool CreateScopes { get; set; } = false;
}
