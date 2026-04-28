using SlideGenerator.Application.Modules.Settings.Interfaces;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Modules.Workflows.DSL.Nodes;
using SlideGenerator.Application.Services.Generating.Models.Images;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Services.Generating.Workflows.Activities;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Application.Services.Scanning.Workflows.Activities;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Generating.Workflows;

/// <summary>
///     Defines the end-to-end slide generation pipeline: scanning source files, simplifying instructions,
///     downloading and editing images per row, compositing each slide, and saving the result.
/// </summary>
public sealed class GeneratingWorkflow : IWorkflowDefinition<WorkflowTask>
{
    /// <inheritdoc />
    public string Id => nameof(GeneratingWorkflow);

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public WorkflowNode Build() => new SequenceNode([
        // 1. Parallel initial scans — all unique workbook and presentation files scanned concurrently
        new ParallelNode([
            new ForEachNode<WorkbookIdentifier, WorkflowTask>(
                VariablesDeclaration.WorkbookItem,
                ctx => ctx.Data.Request.Graph.Keys
                    .Select(ws => ws.Workbook)
                    .DistinctBy(wb => Path.GetFullPath(wb.FilePath), StringComparer.OrdinalIgnoreCase),
                Parallel: true,
                Body: new SlotGatedNode(SlotType.ScanWorkbook, new ActivityNode<ScanWorkbook>())
            ),
            new ForEachNode<PresentationIdentifier, WorkflowTask>(
                VariablesDeclaration.PresentationItem,
                ctx => ctx.Data.Request.Graph.Values
                    .Select(slide => slide.Presentation)
                    .DistinctBy(p => Path.GetFullPath(p.FilePath), StringComparer.OrdinalIgnoreCase),
                Parallel: true,
                Body: new SlotGatedNode(SlotType.ScanPresentation, new ActivityNode<ScanPresentation>())
            )
        ]),

        // 2. Populate worksheet keys — only worksheets confirmed present in the scanned workbook
        new InlineNode<WorkflowTask>(ctx =>
        {
            ctx.Data.WorksheetKeys = [.. ctx.Data.Request.Graph.Keys
                .Where(ws =>
                {
                    var path = Path.GetFullPath(ws.Workbook.FilePath);
                    return ctx.Data.WorkbookSummaries.TryGetValue(path, out var summary)
                           && summary.Worksheets.Any(w =>
                               string.Equals(w.Name, ws.Name, StringComparison.OrdinalIgnoreCase));
                })];
            return Task.CompletedTask;
        }),

        // 3. Parallel worksheet loop — each branch is slot-gated by SlotType.Worksheet
        new ForEachNode<WorksheetIdentifier, WorkflowTask>(
            VariablesDeclaration.WorksheetItem,
            ctx => ctx.Data.WorksheetKeys,
            Parallel: true,
            Body: new SlotGatedNode(SlotType.Worksheet, new SequenceNode([

                // Init per-worksheet state entry
                new InlineNode<WorkflowTask>(ctx =>
                {
                    var ws = ctx.GetVariable(VariablesDeclaration.WorksheetItem);
                    ctx.Data.SheetTasks[ws] = new SheetTask { Identifier = ws };
                    return Task.CompletedTask;
                }),

                // Skip entirely if the workbook file is missing
                new ConditionNode<WorkflowTask>(
                    ctx => File.Exists(ctx.GetVariable(VariablesDeclaration.WorksheetItem).Workbook.FilePath),
                    Then: new SequenceNode([

                        new ActivityNode<CreateWorkingPresentation>(),

                        new ActivityNode<SimplyInstructions>(),

                        // 4. Sequential row loop
                        new ForEachNode<RowIdentifier, WorkflowTask>(
                            VariablesDeclaration.RowItem,
                            ctx =>
                            {
                                var ws = ctx.GetVariable(VariablesDeclaration.WorksheetItem);
                                return ctx.Data.SheetTasks[ws].RowIndices
                                    .Select(r => new RowIdentifier(ws, r));
                            },
                            Parallel: false,
                            Body: new TryNode(new SequenceNode([

                                // Reset accumulated instructions for this row before downloading
                                new InlineNode<WorkflowTask>(ctx =>
                                {
                                    var rc = ctx.GetVariable(VariablesDeclaration.RowItem);
                                    ctx.Data.SheetTasks[rc.Worksheet].RowSpecializedInstructions[rc.Index] = [];
                                    return Task.CompletedTask;
                                }),

                                // Parallel image downloads, each throttled by SlotType.Download
                                new ForEachNode<RowTask, WorkflowTask>(
                                    VariablesDeclaration.RowTaskItem,
                                    ctx =>
                                    {
                                        var rc = ctx.GetVariable(VariablesDeclaration.RowItem);
                                        var sheetTask = ctx.Data.SheetTasks[rc.Worksheet];
                                        return sheetTask.RowImageInstructions
                                            .Select(instr => new RowTask(rc.Worksheet, rc.Index, instr));
                                    },
                                    Parallel: true,
                                    Body: new SlotGatedNode(SlotType.Download,
                                        new ActivityNode<DownloadImage>())
                                ),

                                // Parallel image edits, each throttled by SlotType.EditImage
                                new ForEachNode<RowTask, WorkflowTask>(
                                    VariablesDeclaration.RowTaskItem,
                                    ctx =>
                                    {
                                        var rc = ctx.GetVariable(VariablesDeclaration.RowItem);
                                        var sheetTask = ctx.Data.SheetTasks[rc.Worksheet];
                                        var settings = ctx.GetRequiredService<ISettingProvider>();
                                        var downloadRoot = Path.GetFullPath(settings.Current.Download.DownloadFolder);
                                        return (sheetTask.RowSpecializedInstructions.GetValueOrDefault(rc.Index) ?? [])
                                            .Select(instr => (instr,
                                                filePath: ImagePathRules.ScanDownloadedFile(
                                                    instr.GetDownloadPath(downloadRoot, rc.Worksheet, rc.Index))))
                                            .Where(x => x.filePath != null)
                                            .Select(x => new RowTask(rc.Worksheet, rc.Index, null,
                                                new KeyValuePair<SpecializedInstruction, string>(x.instr, x.filePath!)));
                                    },
                                    Parallel: true,
                                    Body: new SlotGatedNode(SlotType.EditImage,
                                        new ActivityNode<EditImage>())
                                ),

                                // Slide editing is serialized by SlotType.EditSlide
                                new SlotGatedNode(SlotType.EditSlide, new SequenceNode([
                                    new ActivityNode<CloneTemplateSlide>(),
                                    new ActivityNode<EditSlide>()
                                ]))
                            ]))
                        ),

                        new ActivityNode<RemoveWorkingTemplateSlide>(),

                        // Release presentation lease
                        new InlineNode<WorkflowTask>(ctx =>
                        {
                            var ws = ctx.GetVariable(VariablesDeclaration.WorksheetItem);
                            if (ctx.Data.SheetTasks.TryGetValue(ws, out var sheetTask))
                                sheetTask.PresentationLease?.Dispose();
                            return Task.CompletedTask;
                        })
                    ])
                )
            ]))
        )
    ]);
}
