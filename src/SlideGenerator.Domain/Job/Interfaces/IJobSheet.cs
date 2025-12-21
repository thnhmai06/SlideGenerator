using SlideGenerator.Domain.Job.Enums;

namespace SlideGenerator.Domain.Job.Interfaces;

/// <summary>
///     Exposes a read-only view of a sheet job.
/// </summary>
public interface IJobSheet
{
    string Id { get; }
    string GroupId { get; }
    string SheetName { get; }
    string OutputPath { get; }
    SheetJobStatus Status { get; }
    int CurrentRow { get; }
    int TotalRows { get; }
    float Progress { get; }
    int ErrorCount { get; }
    string? ErrorMessage { get; }
}