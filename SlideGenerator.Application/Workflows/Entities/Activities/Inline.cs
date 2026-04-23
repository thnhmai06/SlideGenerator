using SlideGenerator.Application.Workflows.Entities.Contexts;

namespace SlideGenerator.Application.Workflows.Entities.Activities;

/// <summary>
///     Executes an asynchronous function inline.
/// </summary>
/// <remarks>
///     Describes a leaf activity whose work is expressed as an inline async delegate.
///     Infrastructure provides a concrete executable form.
/// </remarks>
public abstract class Inline : Activity
{
    /// <summary>Gets the delegate to invoke when this activity executes.</summary>
    public required Func<IExecutionContext, CancellationToken, ValueTask> Function { get; init; }
}
