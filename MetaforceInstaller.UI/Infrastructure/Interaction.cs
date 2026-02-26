using System;
using System.Threading.Tasks;

namespace MetaforceInstaller.UI.Infrastructure;

/// <summary>
/// Simple implementation of Interaction pattern from ReactiveUI framework.
/// https://www.reactiveui.net/docs/handbook/interactions/
/// </summary>
public sealed class Interaction<TInput, TOutput> : IDisposable
{
    // this is a reference to the registered interaction handler.
    private Func<TInput, Task<TOutput>>? _handler;

    /// <summary>
    /// Performs the requested interaction <see langword="async"/>. Returns the result provided by the View
    /// </summary>
    public Task<TOutput> HandleAsync(TInput input)
    {
        if (_handler is null)
            throw new InvalidOperationException("Handler wasn't registered");

        return _handler(input);
    }

    /// <summary>
    /// Registers a handler to our Interaction
    /// </summary>
    public IDisposable RegisterHandler(Func<TInput, Task<TOutput>> handler)
    {
        if (_handler is not null)
            throw new InvalidOperationException("Handler was already registered");

        _handler = handler;
        return this;
    }

    public void Dispose()
    {
        _handler = null;
    }
}