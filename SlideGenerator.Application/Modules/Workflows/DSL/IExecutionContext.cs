using SlideGenerator.Application.Modules.Workflows.Models.States;

namespace SlideGenerator.Application.Modules.Workflows.DSL;

/// <summary>
///     Base execution context passed to all nodes and activities during interpreter dispatch.
///     Serves as the memory/environment for the current scope in the lexical scope chain.
///     Typed activities receive <see cref="IExecutionContext{TData}" />, which extends this interface.
/// </summary>
public interface IExecutionContext
{
    /// <summary>Gets the cancellation token for the current execution.</summary>
    CancellationToken CancellationToken { get; }

    /// <summary>Gets the execution snapshot tree for logging and status tracking.</summary>
    ExecutionSnapshot Snapshot { get; }

    /// <summary>Gets the service provider for dependency resolution.</summary>
    IServiceProvider Services { get; }

    /// <summary>
    ///     Reads the value of <paramref name="key" /> from this scope.
    ///     If not found locally, the lookup walks up the parent scope chain.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the variable is not defined in any ancestor scope.</exception>
    TVar GetVariable<TVar>(Handle<TVar> key);

    /// <summary>Writes <paramref name="value" /> into this scope's local variable dictionary.</summary>
    void SetVariable<TVar>(Handle<TVar> key, TVar value);

    /// <summary>
    ///     Attempts to read the value of <paramref name="key" /> from this scope or its parents.
    /// </summary>
    /// <returns>True if found; otherwise, false.</returns>
    bool TryGetVariable<TVar>(Handle<TVar> key, out TVar value);
}

/// <summary>
///     Typed extension of <see cref="IExecutionContext" /> that exposes workflow data as
///     <typeparamref name="TData" /> without an explicit cast.
/// </summary>
/// <typeparam name="TData">The workflow data type (covariant).</typeparam>
public interface IExecutionContext<out TData> : IExecutionContext
{
    /// <summary>Gets the workflow data typed as <typeparamref name="TData" />.</summary>
    TData Data { get; }

    /// <summary>
    ///     Creates a child scope that inherits <see cref="Data" />, <see cref="IExecutionContext.Snapshot" />,
    ///     and DI services from this scope but maintains its own local variable dictionary.
    ///     Used by the interpreter when entering a <c>ForEachNode</c> branch so that each
    ///     iteration's variables are physically isolated without relying on thread-local storage.
    /// </summary>
    IExecutionContext<TData> CreateChildScope();
}