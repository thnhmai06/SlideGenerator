namespace SlideGenerator.Application.Modules.Workflows.Models.States;

/// <summary>
///     Typed variable storage for a workflow execution scope.
///     Provides named get/set for checkpoint-able values associated with an
///     <see cref="SlideGenerator.Application.Modules.Workflows.Models.States.ExecutionSnapshot" />.
/// </summary>
public interface IExecutionPayload
{
    /// <summary>
    ///     Reads a named variable from the current scope, returning <see langword="default" /> if not found.
    /// </summary>
    T? GetVariable<T>(string name);

    /// <summary>Stores a named variable in the current scope.</summary>
    void SetVariable<T>(string name, T value);
}