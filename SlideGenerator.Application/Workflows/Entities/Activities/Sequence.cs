namespace SlideGenerator.Application.Workflows.Entities.Activities;

/// <summary>
///     Executes a list of child activities sequentially.
/// </summary>
/// <remarks>
///     Infrastructure provides a concrete executable form.
/// </remarks>
public abstract class Sequence : Activity
{
    /// <summary>Gets the ordered list of child activities to execute sequentially.</summary>
    public required ICollection<Activity> Activities { get; init; }
}