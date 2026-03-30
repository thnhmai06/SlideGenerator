using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Domain.Slide.Entities;

namespace SlideGenerator.Application.Tasks.Activities;

/// <summary>
///     Persists current transient presentation document to disk.
/// </summary>
/// <remarks>
///     <para>
///         The file path is expected to be the same path previously created by <c>CreatePresentation</c>.
///     </para>
/// </remarks>
public sealed class SavePresentationDocument : Activity
{
    /// <summary>
    ///     Full path of target presentation.
    /// </summary>
    public Input<string> PresentationPath { get; set; } = null!;

    /// <summary>
    ///     Optional flag to close presentation in registry after save.
    /// </summary>
    public Input<bool> CloseAfterSave { get; set; } = new(false);

    /// <summary>
    ///     Output save result.
    /// </summary>
    public Output<bool> Saved { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the slide registry dependency (injected by DI).
    /// </summary>
    public IRegistry<IPresentation> SlideRegistry { get; set; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var presentationPath = context.Get(PresentationPath);
        if (string.IsNullOrWhiteSpace(presentationPath))
        {
            context.Set(Saved, false);
            return ValueTask.CompletedTask;
        }

        var presentation = SlideRegistry.GetOrOpen(presentationPath, isEditable: true);

        presentation.Save();
        context.Set(Saved, true);

        if (!context.Get(CloseAfterSave))
            return ValueTask.CompletedTask;

        SlideRegistry.Close(presentationPath);
        return ValueTask.CompletedTask;
    }
}