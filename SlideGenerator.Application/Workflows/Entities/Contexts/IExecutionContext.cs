namespace SlideGenerator.Application.Workflows.Entities.Contexts;

/// <summary>
///     Provides a standard interface for the workflow execution context.
///     Activities use this to access workflow-scoped variables and state.
/// </summary>
public interface IExecutionContext
{
    /// <summary>Gets the unique ID of the current workflow instance.</summary>
    string WorkflowInstanceId { get; }

    /// <summary>Gets all variables in the current context.</summary>
    IEnumerable<Variable> Variables { get; }

    /// <summary>Gets a variable value by name.</summary>
    /// <typeparam name="T">The type of the variable value.</typeparam>
    /// <param name="name">The name of the variable to retrieve.</param>
    /// <returns>The value of the variable if found; otherwise, <see langword="null" />.</returns>
    T? GetVariable<T>(string name) where T : notnull;

    /// <summary>Sets a variable value by name.</summary>
    /// <typeparam name="T">The type of the variable value.</typeparam>
    /// <param name="name">The name of the variable to set.</param>
    /// <param name="value">The value to assign to the variable.</param>
    void SetVariable<T>(string name, T? value) where T : notnull;

    /// <summary>Checks if a variable exists in the current context.</summary>
    /// <param name="name">The name of the variable to check.</param>
    /// <returns><see langword="true" /> if the variable exists; otherwise, <see langword="false" />.</returns>
    bool HasVariable(string name);

    /// <summary>Removes a variable from the current context.</summary>
    /// <param name="name">The name of the variable to remove.</param>
    void RemoveVariable(string name);
}