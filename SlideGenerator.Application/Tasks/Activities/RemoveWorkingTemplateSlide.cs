using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Domain.Slide.Entities;

namespace SlideGenerator.Application.Tasks.Activities;

/// <summary>
///     Removes a slide from presentation by 1-based index.
/// </summary>
/// <remarks>
///     <para>
///         This activity resolves target presentation through registry by file path.
///     </para>
/// </remarks>
public sealed class RemoveWorkingTemplateSlide : Activity
{
    /// <summary>
    ///     Full path of target presentation.
    /// </summary>
    public Input<string> PresentationPath { get; set; } = null!;

    /// <summary>
    ///     1-based index of template slide to remove.
    /// </summary>
    public Input<int> TemplateSlideIndex { get; set; } = new(1);

    /// <summary>
    ///     Output whether a slide was removed.
    /// </summary>
    public Output<bool> Removed { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the slide registry dependency (injected by DI).
    /// </summary>
    public IRegistry<IPresentation> SlideRegistry { get; set; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var presentationPath = context.Get(PresentationPath);
        var slideIndex = context.Get(TemplateSlideIndex);
        if (slideIndex <= 0 || string.IsNullOrWhiteSpace(presentationPath))
        {
            context.Set(Removed, false);
            return ValueTask.CompletedTask;
        }

        var presentation = SlideRegistry.GetOrOpen(presentationPath, isEditable: true);

        context.Set(Removed, presentation.RemoveSlide(slideIndex));
        return ValueTask.CompletedTask;
    }
}