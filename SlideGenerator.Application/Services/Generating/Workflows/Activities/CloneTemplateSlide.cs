using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Modules.Workflows.Rules;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Entities.Presentation;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     Duplicates the working template slide to the position reserved for the current row
///     (<c>templateIndex + rowIndex</c>), creating the target slide that will be edited by
///     <see cref="EditSlide" />.
/// </summary>
/// <remarks>
///     <b>Variables read:</b> <see cref="VariablesDeclaration.RowItem" />.<br/>
///     <b>Data read:</b> <see cref="SheetTask.WorkingTemplateSlide" />.<br/>
///     <b>Services:</b> <see cref="FileRegistry{IPresentation}" />.<br/>
///     <b>CancellationToken:</b> propagated to registry acquire.
/// </remarks>
public sealed class CloneTemplateSlide(
    FileRegistry<IPresentation> slideRegistry,
    Variable<RowIdentifier> rowVar) : ILeafActivity<WorkflowTask>
{
    /// <inheritdoc />
    public async Task ExecuteAsync(IActivityContext<WorkflowTask> context)
    {
        var data = context.Data;
        var rc = context.GetVariable(rowVar);
        var sheetTask = data.SheetTasks[rc.Worksheet];

        var slideIdentifier = sheetTask.WorkingTemplateSlide
                              ?? throw new ArgumentException(
                                  "Template slide must be set in context before cloning.");

        using var lease = await slideRegistry
            .AcquireAsync(slideIdentifier.Presentation.FilePath, true, context.CancellationToken)
            .ConfigureAwait(false);

        lease.Value.CopySlide(WorkflowConstants.WorkingTemplateSlideIndex,
            WorkflowConstants.WorkingTemplateSlideIndex + rc.Index);
    }
}
