using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Entities.Presentation;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     Removes the working template slide (index 1) from the presentation once all row slides have
///     been generated, then saves the file in the requested output format.
/// </summary>
/// <remarks>
///     <b>Variables read:</b> <see cref="VariablesDeclaration.WorksheetItem" />.<br />
///     <b>Data read:</b> <see cref="SheetTask.WorkingTemplateSlide" />;
///     <see cref="WorkflowTask.Request" /> (<c>OutputExtension</c>).<br />
///     <b>Services:</b> <see cref="FileRegistry{IPresentation}" />.<br />
///     <b>CancellationToken:</b> propagated to registry acquire.
/// </remarks>
public sealed class RemoveWorkingTemplateSlide(
    FileRegistry<IPresentation> slideRegistry,
    Variable<WorksheetIdentifier> worksheetVar) : ILeafActivity<WorkflowTask>
{
    /// <inheritdoc />
    public async Task ExecuteAsync(IActivityContext<WorkflowTask> context)
    {
        var data = context.Data;
        var worksheet = context.GetVariable(worksheetVar);
        var sheetTask = data.SheetTasks[worksheet];

        var slideIdentifier = sheetTask.WorkingTemplateSlide
                              ?? throw new ArgumentException(
                                  "Working template slide identifier must be set in context.");

        using var lease = await slideRegistry
            .AcquireAsync(slideIdentifier.Presentation.FilePath, true, context.CancellationToken)
            .ConfigureAwait(false);

        lease.Value.RemoveSlide(slideIdentifier.Index);
        lease.Value.Save(data.Request.OutputExtension);
    }
}