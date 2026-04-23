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
    T? GetVariable<T>(string name) where T : notnull;

    /// <summary>Sets a variable value by name.</summary>
    void SetVariable<T>(string name, T? value) where T : notnull;

    /// <summary>Checks if a variable exists in the current context.</summary>
    bool HasVariable(string name);

    /// <summary>Removes a variable from the current context.</summary>
    void RemoveVariable(string name);
}