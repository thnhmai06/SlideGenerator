using SlideGenerator.Services.Generating.Models.Identifiers;
using SlideGenerator.Services.Generating.Steps;
using WorkflowCore.Interface;

namespace SlideGenerator.Services.Generating.Workflows;

/// <summary>
///     Orchestrates the slide generation process strictly using WorkflowCore iterators.
///     Phase A: Validation & Template Setup
///     Phase B: Preparation, Metadata Extraction, Resource Fetching
///     Phase C: Assembly & Finalization
/// </summary>
public sealed class GeneratingWorkflow : IWorkflow<GeneratingData>
{
    /// <inheritdoc />
    public string Id => nameof(GeneratingWorkflow);

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public void Build(IWorkflowBuilder<GeneratingData> builder)
    {
        //! Problem: Thay vì mỗi Activity thực hiện một chu trình Mở/Đóng (I/O) riêng biệt, tôi muốn thực hiện cơ chế: Mở một lần tại Activity bắt đầu, xử lý liên tiếp trên RAM, và chỉ Save/Close một lần duy nhất tại Activity cuối cùng.
        //! Problem: Cấu hình đồng bộ cả RunTime và Workflow.
        //! Problem: Đặt tên folder kèm hash để tránh tranh chấp.
        builder

            #region Phase A: Validation & Template Setup

            .ForEach(data => data.Request.Recipe.Graph)
            .Do(x => x
                .StartWith<ValidateRequest>()
                .Input(step => step.Item,
                    (data, context) => (KeyValuePair<SheetIdentifier, SlideIdentifier>)context.Item)
                .Then<CreateTemplate>()
                .Input(step => step.Item,
                    (data, context) => (KeyValuePair<SheetIdentifier, SlideIdentifier>)context.Item))
            .Then(_ => WorkflowCore.Models.ExecutionResult.Next())

            #endregion

            #region Phase B: Preparing Resources

            #region Phase B.1: Prepare Iteration Tasks

            .ForEach(data => data.ValidWorksheets.Values)
            .Do(x => x
                .StartWith<PrepareIterationTasks>()
                .Input(step => step.Worksheet, (data, context) => context.Item as ValidatedWorksheet))
            .Then(_ => WorkflowCore.Models.ExecutionResult.Next())

            #endregion

            #region Phase B.2: Extract Shape Metadata using WorkflowCore Loop

            .ForEach(data => data.ShapeTasks)
            .Do(x => x
                .StartWith<ExtractShapeBounds>()
                .Input(step => step.Task, (data, context) => context.Item as ShapeTask))
            .Then(_ => WorkflowCore.Models.ExecutionResult.Next())

            #endregion

            #region Phase B.3: Extract Row URIs to Generate ImageTasks

            .ForEach(data => data.RowTasks)
            .Do(x => x
                .StartWith<ExtractRowData>()
                .Input(step => step.Task, (data, context) => context.Item as RowTask))
            .Then(_ => WorkflowCore.Models.ExecutionResult.Next())

            #endregion

            #region Phase B.4: Download & Edit Images

            .ForEach(data => data.ImageTasks)
            .Do(x => x
                .StartWith<DownloadImage>()
                .Input(step => step.Task, (data, context) => context.Item as ImageTask)
                .Then<EditImage>()
                .Input(step => step.Task, (data, context) => context.Item as ImageTask))
            .Then(_ => WorkflowCore.Models.ExecutionResult.Next())

            #endregion

            #endregion

            #region Phase C: Replacing

            #region Phase C.1: Replace Shape Data (Assembly)

            .ForEach(data => data.RowShapeTasks)
            .Do(x => x
                .StartWith<ReplaceShapeData>()
                .Input(step => step.Task, (data, context) => context.Item as RowShapeTask))
            .Then(_ => WorkflowCore.Models.ExecutionResult.Next())

            #endregion

            #region Phase C.2: Finalize

            .ForEach(data => data.ValidWorksheets.Values)
            .Do(x => x
                .StartWith<FinalizePresentation>()
                .Input(step => step.Worksheet, (data, context) => context.Item as ValidatedWorksheet));

        #endregion

        #endregion
    }
}