using System.Collections.Concurrent;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Modules.Workflows.DSL.Nodes;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Services.Scanning.Models;
using SlideGenerator.Application.Services.Scanning.Models.Sheets;
using SlideGenerator.Application.Services.Scanning.Models.Slides;
using SlideGenerator.Application.Services.Scanning.Workflows.Activities;
using SlideGenerator.Domain.Sheets.Models.Identifiers;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Scanning.Workflows;

/// <summary>
///     Defines the standalone scanning workflow for <see cref="ScanningRequest" />.
/// </summary>
public sealed class ScanningWorkflow : IWorkflowDefinition<ScanningRequest>
{
    /// <inheritdoc />
    public string Id => nameof(ScanningWorkflow);

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public WorkflowNode Build()
    {
        return new SequenceNode([
            // 0. Initialize scanning Variables if not already present in the context
            new InlineNode<ScanningRequest>(ctx =>
            {
                if (!ctx.TryGetVariable(ScanningVariables.WorkbookSummaries, out _))
                {
                    ctx.SetVariable(ScanningVariables.WorkbookSummaries,
                        new ConcurrentDictionary<string, WorkbookSummary>(StringComparer.OrdinalIgnoreCase));
                }

                if (!ctx.TryGetVariable(ScanningVariables.PresentationSummaries, out _))
                {
                    ctx.SetVariable(ScanningVariables.PresentationSummaries,
                        new ConcurrentDictionary<string, PresentationSummary>(StringComparer.OrdinalIgnoreCase));
                }

                return Task.CompletedTask;
            }),

            // 1. Parallel initial scans
            new ParallelNode([
                new ForEachNode<WorkbookIdentifier, ScanningRequest>(
                    ScanningVariables.WorkbookItem,
                    ctx => ctx.Data.Workbooks,
                    true,
                    new GateNode(GateType.ScanWorkbook, new ActivityNode<ScanWorkbook>())
                ),
                new ForEachNode<PresentationIdentifier, ScanningRequest>(
                    ScanningVariables.PresentationItem,
                    ctx => ctx.Data.Presentations,
                    true,
                    new GateNode(GateType.ScanPresentation, new ActivityNode<ScanPresentation>())
                )
            ])
        ]);
    }
}
