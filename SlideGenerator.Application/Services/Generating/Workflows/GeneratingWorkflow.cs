using SlideGenerator.Application.Modules.Settings.Interfaces;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Modules.Workflows.DSL.Nodes;
using SlideGenerator.Application.Services.Generating.Models;
using SlideGenerator.Application.Services.Generating.Models.Images;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Services.Generating.Workflows.Activities;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Application.Services.Scanning.Workflows;
using SlideGenerator.Domain.Sheets.Models.Identifiers;

namespace SlideGenerator.Application.Services.Generating.Workflows;

/// <summary>
///     Defines the end-to-end slide generation pipeline: scanning source files, simplifying instructions,
///     downloading and editing images per row, compositing each slide, and saving the result.
/// </summary>
public sealed class GeneratingWorkflow(ISettingProvider settingProvider) : IWorkflowDefinition<GeneratingRequest>
{
    /// <inheritdoc />
    public string Id => nameof(GeneratingWorkflow);

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public WorkflowNode Build()
    {
        var scanningWorkflow = new ScanningWorkflow();
        
        return new SequenceNode([
            // 0. Initial scans — all unique workbook and presentation files scanned concurrently
            scanningWorkflow.Build(),

            // 1. Populate WorksheetKeys Variable — filter only worksheets confirmed present in their workbook
            new InlineNode<GeneratingRequest>(ctx =>
            {
                var summaries = ctx.GetVariable(VariablesDeclaration.WorkbookSummaries);
                ctx.SetVariable(VariablesDeclaration.WorksheetKeys,
                [
                    .. ctx.Data.Graph.Keys
                        .Where(ws =>
                        {
                            var path = Path.GetFullPath(ws.Workbook.FilePath);
                            return summaries.TryGetValue(path, out var summary)
                                   && summary.Worksheets.Any(w =>
                                       string.Equals(w.Name, ws.Name, StringComparison.OrdinalIgnoreCase));
                        })
                ]);
                return Task.CompletedTask;
            }),

            // 3. Parallel worksheet loop — each branch is slot-gated by GateType.Worksheet
            new ForEachNode<WorksheetIdentifier, GeneratingRequest>(
                VariablesDeclaration.WorksheetItem,
                ctx => ctx.GetVariable(VariablesDeclaration.WorksheetKeys),
                true,
                new GateNode(GateType.Worksheet,
                    // Skip entirely if the workbook file is missing
                    new ConditionNode<GeneratingRequest>(
                        ctx => File.Exists(ctx.GetVariable(VariablesDeclaration.WorksheetItem).Workbook.FilePath),
                        new SequenceNode([
                            new ActivityNode<CreateWorkingPresentation>(),

                            new ActivityNode<SimplyInstructions>(),

                            // Initialize a row instructions map for this worksheet
                            new InlineNode<GeneratingRequest>(ctx =>
                            {
                                ctx.SetVariable(VariablesDeclaration.RowInstructionsMap,
                                    new Dictionary<int, List<SpecializedInstruction>>());
                                return Task.CompletedTask;
                            }),

                            // Phase A: All rows — Download and Edit Images
                            new ForEachNode<RowIdentifier, GeneratingRequest>(
                                VariablesDeclaration.RowItem,
                                ctx => ctx.GetVariable(VariablesDeclaration.RowIndices)
                                    .Select(r => new RowIdentifier(
                                        ctx.GetVariable(VariablesDeclaration.WorksheetItem), r)),
                                false,
                                new TryNode(new SequenceNode([
                                    // 1. Reset resolved instructions for this row
                                    new InlineNode<GeneratingRequest>(ctx =>
                                    {
                                        ctx.SetVariable(VariablesDeclaration.SpecializedInstructions, []);
                                        return Task.CompletedTask;
                                    }),

                                    // 2. Parallel image downloads, each throttled by GateType.Download
                                    new ForEachNode<RowTask, GeneratingRequest>(
                                        VariablesDeclaration.RowTaskItem,
                                        ctx =>
                                        {
                                            var rc = ctx.GetVariable(VariablesDeclaration.RowItem);
                                            return ctx.GetVariable(VariablesDeclaration.RowImageInstructions)
                                                .Select(instr => new RowTask(rc.Worksheet, rc.Index, instr));
                                        },
                                        true,
                                        new GateNode(GateType.Download,
                                            new ActivityNode<DownloadImage>())
                                    ),

                                    // 3. Save resolved instructions to the worksheet map for Phase B
                                    new InlineNode<GeneratingRequest>(ctx =>
                                    {
                                        var map = ctx.GetVariable(VariablesDeclaration.RowInstructionsMap);
                                        var instr = ctx.GetVariable(VariablesDeclaration.SpecializedInstructions);
                                        map[ctx.GetVariable(VariablesDeclaration.RowItem).Index] = [.. instr];
                                        return Task.CompletedTask;
                                    }),

                                    // 4. Parallel image edits, each throttled by GateType.EditImage
                                    new ForEachNode<RowTask, GeneratingRequest>(
                                        VariablesDeclaration.RowTaskItem,
                                        ctx =>
                                        {
                                            var rc = ctx.GetVariable(VariablesDeclaration.RowItem);
                                            var downloadRoot = Path.GetFullPath(
                                                settingProvider.Current.Download.DownloadFolder);
                                            var rowInstr =
                                                ctx.GetVariable(VariablesDeclaration.RowInstructionsMap)[rc.Index];
                                            return rowInstr
                                                .Select(instr => (instr,
                                                    filePath: ImagePathRules.ScanDownloadedFile(
                                                        instr.GetDownloadPath(downloadRoot, rc.Worksheet, rc.Index))))
                                                .Where(x => x.filePath != null)
                                                .Select(x => new RowTask(rc.Worksheet, rc.Index, null,
                                                    new KeyValuePair<SpecializedInstruction, string>(x.instr,
                                                        x.filePath!)));
                                        },
                                        true,
                                        new GateNode(GateType.EditImage,
                                            new ActivityNode<EditImage>())
                                    )
                                ]))
                            ),

                            // Phase B: All rows — Slide Editing (write lease acquired lazily by CloneTemplateSlide)
                            new GateNode(GateType.EditSlide, new SequenceNode([
                                // Sequential slide editing loop
                                new ForEachNode<RowIdentifier, GeneratingRequest>(
                                    VariablesDeclaration.RowItem,
                                    ctx => ctx.GetVariable(VariablesDeclaration.RowIndices)
                                        .Select(r => new RowIdentifier(
                                            ctx.GetVariable(VariablesDeclaration.WorksheetItem), r)),
                                    false,
                                    new TryNode(new SequenceNode([
                                        // Restore resolved instructions for this row from Phase A map
                                        new InlineNode<GeneratingRequest>(ctx =>
                                        {
                                            var map = ctx.GetVariable(VariablesDeclaration.RowInstructionsMap);
                                            var idx = ctx.GetVariable(VariablesDeclaration.RowItem).Index;
                                            ctx.SetVariable(VariablesDeclaration.SpecializedInstructions, map[idx]);
                                            return Task.CompletedTask;
                                        }),

                                        new ActivityNode<CloneTemplateSlide>(),
                                        new ActivityNode<EditSlide>()
                                    ]))
                                ),

                                new ActivityNode<RemoveWorkingTemplateSlide>()
                            ]))
                        ])
                    )
                )
            )
        ]);
    }
}