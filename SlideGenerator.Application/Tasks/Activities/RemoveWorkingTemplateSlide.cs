using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Domain.Slide.Entities;
using SlideGenerator.Domain.Slide.Models;

namespace SlideGenerator.Application.Tasks.Activities;

/// <summary>
///     Removes a slide from presentation by 1-based index.
/// </summary>
/// <remarks>
///     <para>
///         This activity resolves target presentation through registry by file path.
///     </para>
/// </remarks>
public sealed class RemoveWorkingTemplateSlide(IRegistry<IPresentation> slideRegistry) : Activity
{
    /// <summary>
    ///    Input working template slide identifier.
    /// </summary>
    public Input<SlideIdentifier> WorkingTemplateSlide { get; set; } = null!;

    /// <summary>
    ///     Output whether a slide was removed.
    /// </summary>
    public Output<bool> Removed { get; set; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var slideIdentifier = context.Get(WorkingTemplateSlide);
        if (slideIdentifier is null)
            throw new InvalidOperationException("Template slide identifier is not provided.");

        var presentation = slideRegistry.GetOrOpen(slideIdentifier.Presentation.FilePath, isEditable: true);
        context.Set(Removed, presentation.RemoveSlide(slideIdentifier.Index));
        return ValueTask.CompletedTask;
    }
}