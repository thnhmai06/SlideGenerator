namespace SlideGenerator.Application.Workflows.Entities;

/// <summary>
///     Base class for all workflow-related entities that can have a name.
/// </summary>
public abstract class Entry
{
    /// <summary>Gets an optional display name used for logging and diagnostics.</summary>
    public string? Name { get; init; }
}