using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Application.Modules.Settings.Interfaces;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Modules.Workflows.DSL.Activities;
using SlideGenerator.Application.Services.Generating.Models;
using SlideGenerator.Application.Services.Generating.Models.Images;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Services.Generating.Workflows.Activities;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Application.Services.Scanning.Models;
using SlideGenerator.Application.Services.Scanning.Workflows;
using SlideGenerator.Domain.Sheets.Models.Identifiers;

namespace SlideGenerator.Application.Services.Generating.Workflows;

/// <summary>
///     Defines the end-to-end slide generation pipeline: scanning source files, simplifying instructions,
///     downloading and editing images per row, compositing each slide, and saving the result.
/// </summary>
public sealed class GeneratingWorkflow(IServiceProvider services) : IWorkflow<GeneratingRequest>
{
    private readonly ISettingProvider _settingProvider = services.GetRequiredService<ISettingProvider>();
    private readonly GateLocker<GateType> _gateLocker = services.GetRequiredService<GateLocker<GateType>>();
    private readonly ScanningWorkflow _scanningWorkflow = services.GetRequiredService<ScanningWorkflow>();

    public GeneratingWorkflow() : this(default!) { }

    /// <inheritdoc />
    public string Id => nameof(GeneratingWorkflow);

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public Activity<GeneratingRequest> Build()
    {
        return new Sequence<GeneratingRequest>
        {
            Steps =
            [
                // 0. Initial scans — all unique workbook and presentation files scanned concurrently
                new Inline<GeneratingRequest>
                {
                    Action = ctx => _scanningWorkflow.Build().ExecuteAsync(ctx as IExecutionContext<ScanningRequest> ?? throw new InvalidOperationException("Context mismatch"))
                },

                // 1. Populate WorksheetKeys Variable — filter only worksheets confirmed present in their workbook
                new Inline<GeneratingRequest>
                {
                    Action = ctx =>
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
                    }
                },

                // 3. Parallel worksheet loop — each branch is slot-gated by GateType.Worksheet
                new ForEach<WorksheetIdentifier, GeneratingRequest>(true)
                {
                    Items = ctx => ctx.GetVariable(VariablesDeclaration.WorksheetKeys),
                    Handle = VariablesDeclaration.WorksheetItem,
                    Body = new GateWrapper<GateType, GeneratingRequest>(_gateLocker)
                    {
                        Gate = GateType.Worksheet,
                        Body = new Condition<GeneratingRequest>
                        {
                            // Skip entirely if the workbook file is missing
                            Predicate = ctx => File.Exists(ctx.GetVariable(VariablesDeclaration.WorksheetItem).Workbook.FilePath),
                            Then = new Sequence<GeneratingRequest>
                            {
                                Steps =
                                [
                                    Inline<GeneratingRequest>.Activity<CreateWorkingPresentation>(),
                                    Inline<GeneratingRequest>.Activity<SimplyInstructions>(),

                                    // Initialize a row instructions map for this worksheet
                                    new Inline<GeneratingRequest>
                                    {
                                        Action = ctx =>
                                        {
                                            ctx.SetVariable(VariablesDeclaration.RowInstructionsMap,
                                                new Dictionary<int, List<SpecializedInstruction>>());
                                            return Task.CompletedTask;
                                        }
                                    },

                                    // Phase A: All rows — Download and Edit Images
                                    new ForEach<RowIdentifier, GeneratingRequest>(false)
                                    {
                                        Items = ctx => ctx.GetVariable(VariablesDeclaration.RowIndices)
                                            .Select(r => new RowIdentifier(
                                                ctx.GetVariable(VariablesDeclaration.WorksheetItem), r)),
                                        Handle = VariablesDeclaration.RowItem,
                                        Body = new Try<GeneratingRequest>
                                        {
                                            Body = new Sequence<GeneratingRequest>
                                            {
                                                Steps =
                                                [
                                                    // 1. Reset resolved instructions for this row
                                                    new Inline<GeneratingRequest>
                                                    {
                                                        Action = ctx =>
                                                        {
                                                            ctx.SetVariable(VariablesDeclaration.SpecializedInstructions, []);
                                                            return Task.CompletedTask;
                                                        }
                                                    },

                                                    // 2. Parallel image downloads, each throttled by GateType.Download
                                                    new ForEach<RowTask, GeneratingRequest>(true)
                                                    {
                                                        Items = ctx =>
                                                        {
                                                            var rc = ctx.GetVariable(VariablesDeclaration.RowItem);
                                                            return ctx.GetVariable(VariablesDeclaration.RowImageInstructions)
                                                                .Select(instr => new RowTask(rc.Worksheet, rc.Index, instr));
                                                        },
                                                        Handle = VariablesDeclaration.RowTaskItem,
                                                        Body = new GateWrapper<GateType, GeneratingRequest>(_gateLocker)
                                                        {
                                                            Gate = GateType.Download,
                                                            Body = Inline<GeneratingRequest>.Activity<DownloadImage>()
                                                        }
                                                    },

                                                    // 3. Save resolved instructions to the worksheet map for Phase B
                                                    new Inline<GeneratingRequest>
                                                    {
                                                        Action = ctx =>
                                                        {
                                                            var map = ctx.GetVariable(VariablesDeclaration.RowInstructionsMap);
                                                            var instr = ctx.GetVariable(VariablesDeclaration.SpecializedInstructions);
                                                            map[ctx.GetVariable(VariablesDeclaration.RowItem).Index] = [.. instr];
                                                            return Task.CompletedTask;
                                                        }
                                                    },

                                                    // 4. Parallel image edits, each throttled by GateType.EditImage
                                                    new ForEach<RowTask, GeneratingRequest>(true)
                                                    {
                                                        Items = ctx =>
                                                        {
                                                            var rc = ctx.GetVariable(VariablesDeclaration.RowItem);
                                                            var downloadRoot = Path.GetFullPath(
                                                                _settingProvider.Current.Download.DownloadFolder);
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
                                                        Handle = VariablesDeclaration.RowTaskItem,
                                                        Body = new GateWrapper<GateType, GeneratingRequest>(_gateLocker)
                                                        {
                                                            Gate = GateType.EditImage,
                                                            Body = Inline<GeneratingRequest>.Activity<EditImage>()
                                                        }
                                                    }
                                                ]
                                            }
                                        }
                                    },

                                    // Phase B: All rows — Slide Editing (write lease acquired lazily by CloneTemplateSlide)
                                    new GateWrapper<GateType, GeneratingRequest>(_gateLocker)
                                    {
                                        Gate = GateType.EditSlide,
                                        Body = new Sequence<GeneratingRequest>
                                        {
                                            Steps =
                                            [
                                                // Sequential slide editing loop
                                                new ForEach<RowIdentifier, GeneratingRequest>(false)
                                                {
                                                    Items = ctx => ctx.GetVariable(VariablesDeclaration.RowIndices)
                                                        .Select(r => new RowIdentifier(
                                                            ctx.GetVariable(VariablesDeclaration.WorksheetItem), r)),
                                                    Handle = VariablesDeclaration.RowItem,
                                                    Body = new Try<GeneratingRequest>
                                                    {
                                                        Body = new Sequence<GeneratingRequest>
                                                        {
                                                            Steps =
                                                            [
                                                                // Restore resolved instructions for this row from Phase A map
                                                                new Inline<GeneratingRequest>
                                                                {
                                                                    Action = ctx =>
                                                                    {
                                                                        var map = ctx.GetVariable(VariablesDeclaration.RowInstructionsMap);
                                                                        var idx = ctx.GetVariable(VariablesDeclaration.RowItem).Index;
                                                                        ctx.SetVariable(VariablesDeclaration.SpecializedInstructions, map[idx]);
                                                                        return Task.CompletedTask;
                                                                    }
                                                                },

                                                                Inline<GeneratingRequest>.Activity<CloneTemplateSlide>(),
                                                                Inline<GeneratingRequest>.Activity<EditSlide>()
                                                            ]
                                                        }
                                                    }
                                                },

                                                Inline<GeneratingRequest>.Activity<RemoveWorkingTemplateSlide>()
                                            ]
                                        }
                                    }
                                ]
                            }
                        }
                    }
                }
            ]
        };
    }
}