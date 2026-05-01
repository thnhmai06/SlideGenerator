using SlideGenerator.Application.Modules.Lock.Steps;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Services.Generating.Workflows.Activities;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Domain.Sheets.Models.Identifiers;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using ImageSpecializedInstruction = SlideGenerator.Application.Services.Generating.Models.Images.SpecializedInstruction;

namespace SlideGenerator.Application.Services.Generating.Workflows;

/// <summary>
///     Defines the complex workflow for generating automated presentations from Excel data.
/// </summary>
/// <remarks>
///     The pipeline is structured into five sequential phases to optimize resource usage and ensure consistency:
///     <list type="bullet">
///         <item>
///             <term>Phase 1: Worksheet Setup</term>
///             <description>
///                 Iterates through requested worksheets. For each, it creates a "working presentation" (a copy of the template) 
///                 and resolves placeholders and headers into instructions.
///             </description>
///         </item>
///         <item>
///             <term>Phase 2: Download Tasks</term>
///             <description>
///                 Collects all image instructions across all worksheets and rows. Downloads images in parallel 
///                 using a semaphore to control concurrent network traffic.
///             </description>
///         </item>
///         <item>
///             <term>Phase 3: Image Edit Tasks</term>
///             <description>
///                 Processes downloaded images (decoding and ROI-based cropping) to fit the target slide shapes. 
///                 Operates in parallel with concurrency limits.
///             </description>
///         </item>
///         <item>
///             <term>Phase 4: Slide Generation Tasks</term>
///             <description>
///                 For each data row, clones the template slide within the working presentation and injects 
///                 resolved text and processed images.
///             </description>
///         </item>
///         <item>
///             <term>Phase 5: Finalization</term>
///             <description>
///                 Removes the original template slide from each working presentation and performs final saves.
///             </description>
///         </item>
///     </list>
///     Error resilience: Each activity captures exceptions, allowing the workflow to continue processing valid rows 
///     even if specific files or network requests fail.
/// </remarks>
public sealed class GeneratingWorkflow : IWorkflow<GeneratingData>
{
    public string Id => nameof(GeneratingWorkflow);
    public int Version => 1;

    public void Build(IWorkflowBuilder<GeneratingData> builder)
    {
        builder
            .StartWith(ctx =>
            {
                var data = (GeneratingData)ctx.Workflow.Data;
                data.WorksheetKeys = data.Request.Graph.Keys
                    .Where(ws =>
                    {
                        var path = Path.GetFullPath(ws.Workbook.FilePath);
                        return data.WorkbookSummaries.TryGetValue(path, out var summary)
                               && summary.Worksheets.Any(w =>
                                   string.Equals(w.Name, ws.Name, StringComparison.OrdinalIgnoreCase));
                    })
                    .ToList();
                return ExecutionResult.Next();
            })
            // 1. Worksheet Setup
            .ForEach(data => data.WorksheetKeys)
            .Do(wsLoop => wsLoop
                .StartWith<AcquireSlotStep>().Input(step => step.Gate, data => GateType.ReadWorkbook)
                .Then<CreateWorkingPresentation>()
                .Input(step => step.Worksheet, (data, context) => (WorksheetIdentifier)context.Item)
                .Input(step => step.Request, data => data.Request)
                .Output((step, data) =>
                {
                    if (step.Exception != null) data.Errors[step.Worksheet.ToString()] = step.Exception;
                    if (!string.IsNullOrEmpty(step.OutputPath))
                        data.WorksheetOutputPaths[step.Worksheet.ToString()] = step.OutputPath;
                })
                .Output((step, data) =>
                {
                    if (step.WorkingTemplateSlide != null)
                        data.WorksheetTemplateSlides[step.Worksheet.ToString()] = step.WorkingTemplateSlide;
                })
                .Then<SimplyInstructions>()
                .Input(step => step.Worksheet, (data, context) => (WorksheetIdentifier)context.Item)
                .Input(step => step.Request, data => data.Request)
                .Input(step => step.WorkbookSummaries, data => data.WorkbookSummaries)
                .Input(step => step.PresentationSummaries, data => data.PresentationSummaries)
                .Output((step, data) =>
                {
                    if (step.Exception != null) data.Errors[$"{step.Worksheet}:Instructions"] = step.Exception;
                    if (step.RowIndices != null)
                        data.WorksheetRowIndices[step.Worksheet.ToString()] = step.RowIndices;
                    if (step.TextInstructions != null)
                        data.WorksheetTextInstructions[step.Worksheet.ToString()] = step.TextInstructions;
                    if (step.ImageInstructions != null)
                        data.WorksheetImageInstructions[step.Worksheet.ToString()] = step.ImageInstructions;
                })
                .Then<ReleaseSlotStep>().Input(step => step.Gate, data => GateType.ReadWorkbook)
            )
            // 2. Build Phase A: Download Tasks
            .Then(ctx =>
            {
                var data = (GeneratingData)ctx.Workflow.Data;
                var dlTasks = (from ws in data.WorksheetKeys
                    let wsKey = ws.ToString()
                    let rowIndices = data.WorksheetRowIndices[wsKey]
                    let imgInstructions = data.WorksheetImageInstructions[wsKey]
                    from rowIndex in rowIndices
                    from instr in imgInstructions
                    select new RowTask(ws, rowIndex, instr)).ToList();
                data.DownloadTasks = dlTasks;
                return ExecutionResult.Next();
            })
            .ForEach(data => data.DownloadTasks)
            .Do(dlLoop => dlLoop
                .StartWith<AcquireSlotStep>().Input(step => step.Gate, data => GateType.DownloadImage)
                .Then<DownloadImage>()
                .Input(step => step.RowTask, (data, context) => (RowTask)context.Item)
                .Output((step, data) =>
                {
                    var task = step.RowTask;
                    var key = $"{task.Worksheet}|{task.RowIndex}";
                    
                    if (step.Exception != null)
                        data.Errors[$"{key}:Download:{task.DownloadItem?.Target.Id}"] = step.Exception;

                    if (step.Result != null)
                    {
                        var list = data.RowResolvedInstructions.GetOrAdd(key, _ => []);
                        lock (list)
                        {
                            list.Add(step.Result);
                        }
                    }
                })
                .Then<ReleaseSlotStep>().Input(step => step.Gate, data => GateType.DownloadImage)
            )
            // 3. Build Phase A: Edit Tasks
            .Then(ctx =>
            {
                var data = (GeneratingData)ctx.Workflow.Data;
                var editTasks = new List<RowTask>();
                var downloadRoot = Path.GetFullPath(data.Request.SaveFolder);
                foreach (var ws in data.WorksheetKeys)
                {
                    var wsKey = ws.ToString();
                    var rowIndices = data.WorksheetRowIndices[wsKey];
                    foreach (var rowIndex in rowIndices)
                    {
                        var key = $"{ws}|{rowIndex}";
                        if (data.RowResolvedInstructions.TryGetValue(key, out var resolved))
                            editTasks.AddRange(from instr in resolved
                                let path = instr.GetDownloadPath(downloadRoot, ws, rowIndex)
                                where File.Exists(path)
                                select new RowTask(ws, rowIndex, null,
                                    new KeyValuePair<ImageSpecializedInstruction, string>(instr, path)));
                    }
                }

                data.EditTasks = editTasks;
                return ExecutionResult.Next();
            })
            .ForEach(data => data.EditTasks)
            .Do(editLoop => editLoop
                .StartWith<AcquireSlotStep>().Input(step => step.Gate, data => GateType.EditImage)
                .Then<EditImage>()
                .Input(step => step.RowTask, (data, context) => (RowTask)context.Item)
                .Output((step, data) =>
                {
                    if (step.Exception != null)
                    {
                        var task = step.RowTask;
                        var key = $"{task.Worksheet}|{task.RowIndex}";
                        data.Errors[$"{key}:Edit:{task.EditItem?.Key.Target.Id}"] = step.Exception;
                    }
                })
                .Then<ReleaseSlotStep>().Input(step => step.Gate, data => GateType.EditImage)
            )
            // 4. Build Phase B: Slide Tasks
            .Then(ctx =>
            {
                var data = (GeneratingData)ctx.Workflow.Data;
                var slideTasks = (from ws in data.WorksheetKeys
                    let wsKey = ws.ToString()
                    let rowIndices = data.WorksheetRowIndices[wsKey]
                    let textInstr = data.WorksheetTextInstructions[wsKey]
                    let outputPath = data.WorksheetOutputPaths[wsKey]
                    let templateSlide = data.WorksheetTemplateSlides[wsKey]
                    from rowIndex in rowIndices
                    let key = $"{ws}|{rowIndex}"
                    select new RowTask(ws, rowIndex)
                    {
                        TextInstructions = textInstr, OutputPath = outputPath, TemplateSlide = templateSlide,
                        ResolvedInstructions = data.RowResolvedInstructions.GetOrAdd(key, _ => [])
                    }).ToList();

                data.SlideTasks = slideTasks;
                return ExecutionResult.Next();
            })
            .ForEach(data => data.SlideTasks)
            .Do(slideLoop => slideLoop
                .StartWith<AcquireSlotStep>().Input(step => step.Gate, data => GateType.EditPresentation)
                .Then<CloneTemplateSlide>()
                .Input(step => step.Row,
                    (data, context) =>
                        new RowIdentifier(((RowTask)context.Item).Worksheet, ((RowTask)context.Item).RowIndex))
                .Input(step => step.OutputPath, (data, context) => ((RowTask)context.Item).OutputPath)
                .Output((step, data) =>
                {
                    if (step.Exception != null)
                        data.Errors[$"{step.Row}:Clone"] = step.Exception;
                })
                .Then<EditSlide>()
                .Input(step => step.Row,
                    (data, context) =>
                        new RowIdentifier(((RowTask)context.Item).Worksheet, ((RowTask)context.Item).RowIndex))
                .Input(step => step.OutputPath, (data, context) => ((RowTask)context.Item).OutputPath)
                .Input(step => step.WorkingTemplateSlide, (data, context) => ((RowTask)context.Item).TemplateSlide)
                .Input(step => step.TextInstructions, (data, context) => ((RowTask)context.Item).TextInstructions)
                .Input(step => step.ResolvedImageInstructions,
                    (data, context) => ((RowTask)context.Item).ResolvedInstructions)
                .Output((step, data) =>
                {
                    if (step.Exception != null)
                        data.Errors[$"{step.Row}:Edit"] = step.Exception;
                })
                .Then<ReleaseSlotStep>().Input(step => step.Gate, data => GateType.EditPresentation)
            )
            // 5. Finalize Worksheets
            .ForEach(data => data.WorksheetKeys)
            .Do(finalizeLoop => finalizeLoop
                .StartWith<RemoveWorkingTemplateSlide>()
                .Input(step => step.Worksheet, (data, context) => (WorksheetIdentifier)context.Item)
                .Input(step => step.OutputPath, (data, context) => data.WorksheetOutputPaths[context.Item.ToString()!])
                .Input(step => step.WorkingTemplateSlide,
                    (data, context) => data.WorksheetTemplateSlides[context.Item.ToString()!])
                .Input(step => step.Request, data => data.Request)
                .Output((step, data) =>
                {
                    if (step.Exception != null)
                        data.Errors[$"{step.Worksheet}:Finalize"] = step.Exception;
                })
            );
    }
}