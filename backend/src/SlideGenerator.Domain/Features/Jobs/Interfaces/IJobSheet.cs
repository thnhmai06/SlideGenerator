using SlideGenerator.Domain.Features.Jobs.Enums;

namespace SlideGenerator.Domain.Features.Jobs.Interfaces;

/// <summary>
///     Exposes a read-only view of a sheet job.
/// </summary>
public interface IJobSheet
{
    /// <summary>
    ///     Unique identifier for the sheet job.
    /// </summary>
    string Id { get; }

    /// <summary>
    ///     Parent group identifier.
    /// </summary>
    string GroupId { get; }

    /// <summary>
    ///     Source worksheet name.
    /// </summary>
    string SheetName { get; }

    /// <summary>
    ///     Output file path for the generated presentation.
    /// </summary>
    string OutputPath { get; }

    /// <summary>
    ///     Current sheet job lifecycle status.
    /// </summary>
    SheetJobStatus Status { get; }

    /// <summary>
    ///     Current processed row index (1-based).
    /// </summary>
    int CurrentRow { get; }

    /// <summary>
    ///     Total rows available in the worksheet.
    /// </summary>
    int TotalRows { get; }

    /// <summary>
    ///     Progress percentage (0-100).
    /// </summary>
    float Progress { get; }

    /// <summary>
    ///     Number of errors encountered so far.
    /// </summary>
    int ErrorCount { get; }

    /// <summary>
    ///     Error message for fatal failures, if any.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    ///     Hangfire background job id, if queued.
    /// </summary>
    string? HangfireJobId { get; }
}