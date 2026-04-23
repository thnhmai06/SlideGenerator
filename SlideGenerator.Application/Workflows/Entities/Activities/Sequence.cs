namespace SlideGenerator.Application.Workflows.Entities.Activities;

/// <summary>
///     Describes sequential execution of a list of child activities.
///     Infrastructure provides a concrete executable form.
/// </summary>
public abstract class Sequence : Activity
{
    /// <summary>Gets the ordered list of child activities to execute sequentially.</summary>
    public required ICollection<Activity> Activities { get; init; }
}
