using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Application.Workflows.DSL;
using SlideGenerator.Application.Workflows.Models.States;

namespace SlideGenerator.Infrastructure.Workflows;

/// <summary>
///     Concrete implementation of <see cref="IActivityContext{TData}" /> that forms one node
///     in a lexical scope chain. Each instance maintains its own local variable dictionary and
///     holds a reference to its parent scope for recursive variable lookup.
/// </summary>
internal sealed class WcInterpreterContext<TData>(
    TData data,
    ExecutionState state,
    IServiceProvider services,
    CancellationToken cancellationToken,
    WcInterpreterContext<TData>? parent = null) : IActivityContext<TData>
{
    private readonly Dictionary<string, object?> _locals = new();

    /// <inheritdoc />
    public TData Data => data;

    /// <inheritdoc />
    public CancellationToken CancellationToken => cancellationToken;

    /// <inheritdoc />
    public ExecutionState State => state;

    /// <inheritdoc />
    public T GetRequiredService<T>() where T : notnull => services.GetRequiredService<T>();

    /// <inheritdoc />
    public TVar GetVariable<TVar>(Variable<TVar> key)
    {
        if (_locals.TryGetValue(key.Name, out var value))
            return (TVar)value!;
        return parent is not null 
            ? parent.GetVariable(key) 
            : throw new KeyNotFoundException($"Variable '{key.Name}' is not defined in the scope chain.");
    }

    /// <inheritdoc />
    public void SetVariable<TVar>(Variable<TVar> key, TVar value) =>
        _locals[key.Name] = value;

    /// <inheritdoc />
    public IActivityContext<TData> CreateChildScope() =>
        new WcInterpreterContext<TData>(data, state, services, cancellationToken, parent: this);
}
