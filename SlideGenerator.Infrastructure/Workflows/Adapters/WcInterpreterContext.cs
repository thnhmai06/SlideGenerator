using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Modules.Workflows.Models.States;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

/// <summary>
///     Concrete implementation of <see cref="IExecutionContext{TData}" /> that forms one node
///     in a lexical scope chain. Each instance maintains its own local variable dictionary and
///     holds a reference to its parent scope for recursive variable lookup.
/// </summary>
internal sealed class WcInterpreterContext<TData>(
    TData data,
    ExecutionSnapshot state,
    CancellationToken cancellationToken,
    IServiceProvider services,
    WcInterpreterContext<TData>? parent = null) : IExecutionContext<TData>
{
    private readonly Dictionary<string, object?> _locals = new();

    /// <inheritdoc />
    public TData Data => data;

    /// <inheritdoc />
    public CancellationToken CancellationToken => cancellationToken;

    /// <inheritdoc />
    public ExecutionSnapshot Snapshot => state;

    /// <inheritdoc />
    public IServiceProvider Services => services;

    /// <inheritdoc />
    public TVar GetVariable<TVar>(Handle<TVar> key)
    {
        if (_locals.TryGetValue(key.Name, out var value))
            return (TVar)value!;
        return parent is not null
            ? parent.GetVariable(key)
            : throw new KeyNotFoundException($"Variable '{key.Name}' is not defined in the scope chain.");
    }

    /// <inheritdoc />
    public void SetVariable<TVar>(Handle<TVar> key, TVar value)
    {
        _locals[key.Name] = value;
    }

    /// <inheritdoc />
    public bool TryGetVariable<TVar>(Handle<TVar> key, out TVar value)
    {
        if (_locals.TryGetValue(key.Name, out var obj))
        {
            value = (TVar)obj!;
            return true;
        }

        if (parent is not null)
            return parent.TryGetVariable(key, out value);

        value = default!;
        return false;
    }

    /// <inheritdoc />
    public IExecutionContext<TData> CreateChildScope()
    {
        return new WcInterpreterContext<TData>(data, state, cancellationToken, services, this);
    }
}