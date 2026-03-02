namespace SlideGenerator.Features.Jobs.Entities.Jobs;

/// <summary>
///     Represents current snapshot of a job.
/// </summary>
/// <param name="JobId">Job identifier.</param>
/// <param name="Status">Current job status.</param>
/// <param name="Progress">Current job progress percentage.</param>
/// <param name="CreatedAt">Job creation timestamp.</param>
/// <param name="UpdatedAt">Last update timestamp.</param>
/// <param name="Message">Current summary message.</param>
/// <param name="Sheets">Per-sheet checkpoint collection.</param>
public sealed record JobSnapshotEntity(
    Guid JobId,
    JobStatusEntity Status,
    double Progress,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? Message,
    IReadOnlyList<SheetCheckpointEntity> Sheets);