namespace SlideGenerator.Application.Workflows.Entities.Contexts;

/// <summary>
///     Provides access to DI services and typed variable storage for a workflow execution scope.
/// </summary>
public interface IExecutionContext
{
    /// <summary>Resolves a required service from the DI container.</summary>
    /// <typeparam name="T">The service type to resolve.</typeparam>
    T GetRequiredService<T>() where T : notnull;

    /// <summary>
    ///     Reads a named variable from the current scope chain, returning <see langword="null" /> if not found.
    /// </summary>
    T? GetVariable<T>(string name);

    /// <summary>Stores a named variable in the current scope.</summary>
    void SetVariable<T>(string name, T value);
}
