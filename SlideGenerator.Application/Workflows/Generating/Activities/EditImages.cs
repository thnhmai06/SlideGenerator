using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Workflows.Generating.Models.Images;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Placeholder activity for image editing after downloads are completed.
/// </summary>
/// <remarks>
///     Current implementation is intentionally no-op and only validates the incoming map.
/// </remarks>
public sealed class EditImages : Activity
{
    /// <summary>
    ///     Input downloaded file paths keyed by specialized image instruction.
    /// </summary>
    public required Input<IReadOnlyDictionary<SpecializedInstruction, string>> DownloadedImagePaths { get; init; }

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var downloadedImagePaths = context.Get(DownloadedImagePaths);
        _ = downloadedImagePaths;

        //TODO: implements image editing logic
        return ValueTask.CompletedTask;
    }
}