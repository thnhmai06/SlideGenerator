using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Generating.Activities;

/// <summary>
///     Removes the working template slide (index 1) from the output presentation once all rows have been processed.
/// </summary>
public sealed class RemoveWorkingTemplateSlide(FileRegistry<IPresentation> slideRegistry) : Activity
{
    /// <inheritdoc />
    public override async ValueTask ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        var slideIdentifier = context.GetVariable<SlideIdentifier>(WorksheetContextRules.WorkingTemplateSlide)
                              ?? throw new InvalidOperationException("Template slide identifier is not provided.");

        using var lease = await slideRegistry
            .AcquireAsync(slideIdentifier.Presentation.FilePath, true, cancellationToken)
            .ConfigureAwait(false);

        lease.Value.RemoveSlide(slideIdentifier.Index);
    }
}
