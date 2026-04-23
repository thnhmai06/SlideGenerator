using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Generating.Activities;

/// <summary>Removes the working template slide.</summary>
/// <remarks>Deletes the template slide after processing all rows.</remarks>
/// <param name="slideRegistry">The presentation file registry.</param>
public sealed class RemoveWorkingTemplateSlide(FileRegistry<IPresentation> slideRegistry) : Activity
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown if template slide identifier is missing.</exception>
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
