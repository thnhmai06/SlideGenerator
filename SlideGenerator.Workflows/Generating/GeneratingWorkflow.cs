using SlideGenerator.Gate.Models;
using SlideGenerator.Workflows.Generating.Activities;
using SlideGenerator.Workflows.Generating.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Workflows.Generating;

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
                                   string.Equals(w.Name, ws.WorksheetName, StringComparison.OrdinalIgnoreCase));
                    })
                    .ToList();
                return ExecutionResult.Next();
            })
            // 1. Worksheet Setup
            .ForEach(data => data.WorksheetKeys)
            .Do(wsLoop => wsLoop
                .StartWith<AcquireSlotStep>().Input(step => step.Gate, data => GateType.ReadWorkbook)
                .Then<CreateWorkingPresentation>()
                .Input(step => step.Worksheet, (data, context) => (WorksheetMapping)context.Item)
                .Input(step => step.Request, data => data.Request)
                .Output((step, data) =>
                {
                    var wsKey = GeneratingData.GetWorksheetKey(step.Worksheet);
                    if (step.Exception != null) data.Errors[wsKey] = step.Exception;
                    if (!string.IsNullOrEmpty(step.OutputPath))
                        data.WorksheetOutputPaths[wsKey] = step.OutputPath;
                })
                .Then<SimplyInstructions>()
                .Input(step => step.Worksheet, (data, context) => (WorksheetMapping)context.Item)
                .Input(step => step.Request, data => data.Request)
                .Input(step => step.WorkbookSummaries, data => data.WorkbookSummaries)
                .Input(step => step.PresentationSummaries, data => data.PresentationSummaries)
                .Output((step, data) =>
                {
                    var wsKey = GeneratingData.GetWorksheetKey(step.Worksheet);
                    if (step.Exception != null) data.Errors[$"{wsKey}:Instructions"] = step.Exception;
                    if (step.RowIndices != null)
                        data.WorksheetRowIndices[wsKey] = step.RowIndices;
                    if (step.TextInstructions != null)
                        data.WorksheetTextInstructions[wsKey] = step.TextInstructions;
                    if (step.ImageInstructions != null)
                        data.WorksheetImageInstructions[wsKey] = step.ImageInstructions;
                })
                .Then<ReleaseSlotStep>().Input(step => step.Gate, data => GateType.ReadWorkbook)
            )
            // 2. Build Phase A: Download Tasks
            .Then(ctx =>
            {
                var data = (GeneratingData)ctx.Workflow.Data;
                var dlTasks = (from ws in data.WorksheetKeys
                    let wsKey = GeneratingData.GetWorksheetKey(ws)
                    let rowIndices = data.WorksheetRowIndices[wsKey]
                    let imgInstructions = data.WorksheetImageInstructions[wsKey]
                    from rowIndex in rowIndices
                    from instr in imgInstructions
                    select new RowTask(ws.Workbook, ws.WorksheetName, rowIndex, instr)).ToList();
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
                    var key = $"{task.Workbook.FilePath}|{task.WorksheetName}|{task.RowIndex}";
                    
                    if (step.Exception != null)
                        data.Errors[$"{key}:Download:{task.DownloadItem?.TargetShapeName}"] = step.Exception;

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
                    var wsKey = GeneratingData.GetWorksheetKey(ws);
                    var rowIndices = data.WorksheetRowIndices[wsKey];
                    var outputPath = data.WorksheetOutputPaths[wsKey];
                    var templateSlideMapping = data.Request.Graph[ws];

                    foreach (var rowIndex in rowIndices)
                    {
                        var key = $"{ws.Workbook.FilePath}|{ws.WorksheetName}|{rowIndex}";
                        if (data.RowResolvedInstructions.TryGetValue(key, out var resolved))
                            editTasks.AddRange(from instr in resolved
                                let path = instr.GetDownloadPath(downloadRoot, ws.Workbook, ws.WorksheetName, rowIndex)
                                where File.Exists(path)
                                select new RowTask(ws.Workbook, ws.WorksheetName, rowIndex, null,
                                    new KeyValuePair<SpecializedInstruction, string>(instr, path))
                                {
                                    OutputPath = outputPath,
                                    TemplateSlideIndex = templateSlideMapping.SlideIndex
                                });
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
                        var key = $"{task.Workbook.FilePath}|{task.WorksheetName}|{task.RowIndex}";
                        data.Errors[$"{key}:Edit:{task.EditItem?.Key.TargetShapeName}"] = step.Exception;
                    }
                })
                .Then<ReleaseSlotStep>().Input(step => step.Gate, data => GateType.EditImage)
            )
            // 4. Build Phase B: Slide Tasks
            .Then(ctx =>
            {
                var data = (GeneratingData)ctx.Workflow.Data;
                var slideTasks = (from ws in data.WorksheetKeys
                    let wsKey = GeneratingData.GetWorksheetKey(ws)
                    let rowIndices = data.WorksheetRowIndices[wsKey]
                    let textInstr = data.WorksheetTextInstructions[wsKey]
                    let outputPath = data.WorksheetOutputPaths[wsKey]
                    let templateSlideMapping = data.Request.Graph[ws]
                    from rowIndex in rowIndices
                    let key = $"{ws.Workbook.FilePath}|{ws.WorksheetName}|{rowIndex}"
                    select new RowTask(ws.Workbook, ws.WorksheetName, rowIndex)
                    {
                        TextInstructions = textInstr, OutputPath = outputPath, 
                        TemplateSlideIndex = templateSlideMapping.SlideIndex + rowIndex, // Offset for cloned slides
                        ResolvedInstructions = data.RowResolvedInstructions.GetOrAdd(key, _ => [])
                    }).ToList();

                data.SlideTasks = slideTasks;
                return ExecutionResult.Next();
            })
            .ForEach(data => data.SlideTasks)
            .Do(slideLoop => slideLoop
                .StartWith<AcquireSlotStep>().Input(step => step.Gate, data => GateType.EditPresentation)
                .Then<CloneTemplateSlide>()
                .Input(step => step.RowIndex, (data, context) => ((RowTask)context.Item).RowIndex)
                .Input(step => step.OutputPath, (data, context) => ((RowTask)context.Item).OutputPath)
                .Output((step, data) =>
                {
                    if (step.Exception != null)
                        data.Errors[$"{step.OutputPath}:Clone:{step.RowIndex}"] = step.Exception;
                })
                .Then<EditSlide>()
                .Input(step => step.RowTask, (data, context) => (RowTask)context.Item)
                .Output((step, data) =>
                {
                    if (step.Exception != null)
                        data.Errors[$"{step.RowTask.OutputPath}:Edit:{step.RowTask.RowIndex}"] = step.Exception;
                })
                .Then<ReleaseSlotStep>().Input(step => step.Gate, data => GateType.EditPresentation)
            )
            // 5. Finalize Worksheets
            .ForEach(data => data.WorksheetKeys)
            .Do(finalizeLoop => finalizeLoop
                .StartWith<RemoveWorkingTemplateSlide>()
                .Input(step => step.OutputPath, (data, context) => data.WorksheetOutputPaths[GeneratingData.GetWorksheetKey((WorksheetMapping)context.Item)])
                .Output((step, data) =>
                {
                    if (step.Exception != null)
                        data.Errors[$"{step.OutputPath}:Finalize"] = step.Exception;
                })
            );
    }
}