namespace SlideGenerator.Application.Workflows.DSL;

/// <summary>
///     Non-generic dispatch interface for all leaf activities.
///     Used by <see cref="Nodes.ActivityNode{T}" /> so the interpreter can invoke activities
///     without knowing their data type at compile time.
/// </summary>
public interface ILeafActivity
{
    /// <summary>Executes the activity with the provided context.</summary>
    Task ExecuteAsync(IActivityContext context);
}

/// <summary>
///     Typed leaf activity interface. Activities should implement this rather than
///     <see cref="ILeafActivity" /> directly so that <see cref="IActivityContext{TData}.Data" />
///     is available without a cast.
/// </summary>
/// <typeparam name="TData">The workflow data type this activity operates on.</typeparam>
public interface ILeafActivity<TData> : ILeafActivity
{
    /// <summary>Executes the activity with a typed context.</summary>
    Task ExecuteAsync(IActivityContext<TData> context);

    /// <summary>Non-generic dispatch — casts context to <see cref="IActivityContext{TData}" /> and delegates.</summary>
    Task ILeafActivity.ExecuteAsync(IActivityContext context) =>
        ExecuteAsync((IActivityContext<TData>)context);
}
