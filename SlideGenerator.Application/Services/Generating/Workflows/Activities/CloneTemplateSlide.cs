using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Modules.Workflows.DSL.Activities;
using SlideGenerator.Application.Services.Generating.Models;
using SlideGenerator.Application.Services.Generating.Models.States;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Domain.Sheets.Models.Identifiers;
using SlideGenerator.Domain.Slides.Entities.Presentation;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     Duplicates the working template slide to the position reserved for the current row
///     (<c>templateIndex + rowIndex</c>), creating the target slide that will be edited by
///     <see cref="EditSlide" />.
/// </summary>
/// <remarks>
///     <b>Variables read:</b> <see cref="VariablesDeclaration.RowItem" />,
///     <see cref="VariablesDeclaration.WorkingTemplateSlide" />,
///     <see cref="VariablesDeclaration.OutputPath" />.<br />
///     <b>State:</b> Lazily acquires a write lease on the working presentation via
///     <see cref="WorksheetContext.PresentationLease" /> if not yet open.
/// </remarks>
public sealed class CloneTemplateSlide(
    FileRegistry<IPresentation> presentationRegistry,
    Handle<RowIdentifier> rowVar) : Activity<GeneratingRequest>
{
    /// <inheritdoc />
    public override async Task ExecuteAsync(IExecutionContext<GeneratingRequest> context)
    {
        var rc = context.GetVariable(rowVar);
        _ = context.GetVariable(VariablesDeclaration.WorkingTemplateSlide)
            ?? throw new ArgumentException(
                "Template slide must be set in context before cloning.");

        var presentation = await GetOrAcquirePresentationAsync(context).ConfigureAwait(false);

        presentation.CopySlide(WorkflowConstants.WorkingTemplateSlideIndex,
            WorkflowConstants.WorkingTemplateSlideIndex + rc.Index);
    }

    private async ValueTask<IPresentation> GetOrAcquirePresentationAsync(IExecutionContext context)
    {
        var state = GetWorksheetSnapshot(context);
        if (state.Context.PresentationLease is null)
        {
            var outputPath = context.GetVariable(VariablesDeclaration.OutputPath);
            state.Context.PresentationLease = await presentationRegistry
                .AcquireAsync(outputPath, true, context.CancellationToken)
                .ConfigureAwait(false);
        }

        return state.Context.PresentationLease.Value;
    }

    internal static WorksheetSnapshot GetWorksheetSnapshot(IExecutionContext context)
    {
        var ws = context.GetVariable(VariablesDeclaration.WorksheetItem);
        return ((GeneratingSnapshot)context.Snapshot)
            .GetWorkbook(ws.Workbook.Name)!
            .GetWorksheet(ws.Name)!;
    }
}