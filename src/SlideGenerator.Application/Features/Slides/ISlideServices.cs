using SlideGenerator.Domain.Features.Jobs.Components;

namespace SlideGenerator.Application.Features.Slides;

/// <summary>
///     Defines slide processing operations for a single row.
/// </summary>
public interface ISlideServices
{
    Task<RowProcessResult> ProcessRowAsync(
        string presentationPath,
        JobTextConfig[] textConfigs,
        JobImageConfig[] imageConfigs,
        Dictionary<string, string?> rowData,
        JobCheckpoint checkpoint,
        CancellationToken cancellationToken);

    void RemoveFirstSlide(string presentationPath);
}

/// <summary>
///     Result information for row processing.
/// </summary>
public sealed record RowProcessResult(
    int TextReplacementCount,
    int ImageReplacementCount,
    int ImageErrorCount,
    IReadOnlyList<string> Errors,
    IReadOnlyList<TextReplacementDetail> TextReplacements,
    IReadOnlyList<ImageReplacementDetail> ImageReplacements);

/// <summary>
///     Details for a text replacement applied to a shape.
/// </summary>
public sealed record TextReplacementDetail(
    uint ShapeId,
    string Placeholder,
    string Value);

/// <summary>
///     Details for an image replacement applied to a shape.
/// </summary>
public sealed record ImageReplacementDetail(
    uint ShapeId,
    string Source);

/// <summary>
///     Provides cooperative pause checkpoints during processing.
/// </summary>
public delegate Task JobCheckpoint(JobCheckpointStage stage, CancellationToken cancellationToken);

/// <summary>
///     Represents checkpoints within a row execution.
/// </summary>
public enum JobCheckpointStage
{
    BeforeRow,
    BeforeCloudResolve,
    AfterCloudResolve,
    BeforeDownload,
    AfterDownload,
    BeforeImageProcess,
    AfterImageProcess,
    BeforeSlideUpdate,
    AfterSlideUpdate,
    BeforePersistState
}