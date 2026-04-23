namespace SlideGenerator.Application.Workflows.Entities;

public abstract class Entry
{
    /// <summary>Gets an optional display name used for logging and diagnostics.</summary>
    public string? Name { get; init; }
}