namespace SlideGenerator.Jobs.Entities.Jobs;

/// <summary>
///     Represents checkpoint state of a sheet in a job.
/// </summary>
/// <param name="SheetName">Sheet name.</param>
/// <param name="OutputPath">Current output path for this sheet.</param>
/// <param name="CurrentRow">Current processed row index.</param>
/// <param name="TotalRows">Total row count for this sheet.</param>
/// <param name="Status">Current sheet status.</param>
/// <param name="Error">Error message when failed, otherwise null.</param>
public sealed record SheetCheckpointEntity(
    string SheetName,
    string OutputPath,
    int CurrentRow,
    int TotalRows,
    JobStatusEntity Status,
    string? Error);