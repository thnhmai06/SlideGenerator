using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Workflows.Generating.Models.Images;
using SlideGenerator.Application.Workflows.Generating.Rules;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Materializes downloaded image paths from workflow transient storage.
/// </summary>
public sealed class SummarizeDownloadedImagePaths : Activity
{
    public Output<IReadOnlyDictionary<SpecializedInstruction, string>> ImagePaths { get; init; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var transient = context.WorkflowExecutionContext.TransientProperties;
        if (transient.TryGetValue(ImagePathStoreKeys.DownloadedImagePaths, out var store) &&
            store is IReadOnlyDictionary<SpecializedInstruction, string> typed)
        {
            context.Set(ImagePaths, typed);
            return ValueTask.CompletedTask;
        }

        context.Set(ImagePaths, new Dictionary<SpecializedInstruction, string>());
        return ValueTask.CompletedTask;
    }
}
