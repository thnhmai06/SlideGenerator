namespace SlideGenerator.Application.Workflows.Entities.Contexts;

/// <summary>
///     Base class for workflow variables.
/// </summary>
public abstract class Variable : Entry;

/// <summary>
///     Holds a single mutable value that participates in the workflow state model.
///     Uses <see cref="AsyncLocal{T}" /> to provide isolation across asynchronous execution flows.
/// </summary>
/// <typeparam name="T">The type of value stored in this variable.</typeparam>
public class Variable<T> : Variable
{
    /// <summary>The async-local storage for the variable value.</summary>
    private readonly AsyncLocal<T?> _local = new();

    /// <summary>Gets or sets the current value of the variable.</summary>
    /// <value>The current value of the variable, scoped to the current asynchronous execution flow.</value>
    public virtual T? Value
    {
        get => _local.Value;
        set => _local.Value = value;
    }
}
