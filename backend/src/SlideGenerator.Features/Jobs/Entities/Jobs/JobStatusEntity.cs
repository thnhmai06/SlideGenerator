namespace SlideGenerator.Features.Jobs.Entities.Jobs;

/// <summary>
///     Represents externally exposed job lifecycle statuses.
/// </summary>
public enum JobStatusEntity
{
    Pending,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}