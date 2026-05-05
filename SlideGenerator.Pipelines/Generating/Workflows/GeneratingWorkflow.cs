using SlideGenerator.Pipelines.Generating.Steps;
using SlideGenerator.Pipelines.Generating.Workflows.Models;
using WorkflowCore.Interface;

namespace SlideGenerator.Pipelines.Generating.Workflows;

/// <summary>
///     Orchestrates the slide generation process strictly using WorkflowCore iterators.
///     Phase A: Validation & Template Setup
///     Phase B: Preparation, Metadata Extraction, Resource Fetching
///     Phase C: Assembly & Finalization
/// </summary>
public sealed class GeneratingWorkflow : IWorkflow<GeneratingTask>
{
    /// <inheritdoc />
    public string Id => nameof(GeneratingWorkflow);

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public void Build(IWorkflowBuilder<GeneratingTask> builder)
    {
        builder

            #region Phase A: Validation & Template Setup

            .ForEach(data =>
                data.Request.Recipe.Nodes.SelectMany(node =>
                    node.Sheets.Select(sheet => new ValidationItem(sheet, node))))
            .Do(x => x
                .StartWith<ValidateRequest>()
                .Input(step => step.Item, (data, context) => (ValidationItem)context.Item)
                .Then<CreateTemplate>()
                .Input(step => step.Item, (data, context) => (ValidationItem)context.Item))
            .Then(_ => WorkflowCore.Models.ExecutionResult.Next())

            #endregion

            #region Phase B: Preparing Resources

            #region Phase B.1: Extract Data (Shapes, Rows, Generate Tasks)

            .ForEach(data => data.ValidWorksheets.Values)
            .Do(x => x
                .StartWith<ExtractData>()
                .Input(step => step.Worksheet, (data, context) => context.Item as SheetTask))
            .Then(_ => WorkflowCore.Models.ExecutionResult.Next())

            #endregion

            #region Phase B.2: Download & Edit Images

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

            #region Phase C.1: Replace Slide Data (Assembly)

            .ForEach(data => data.SlideTasks)
            .Do(x => x
                .StartWith<ReplaceSlideData>()
                .Input(step => step.Task, (data, context) => context.Item as SlideTask))
            .Then(_ => WorkflowCore.Models.ExecutionResult.Next())

            #endregion

            #region Phase C.2: Cleanup

            .Then<CloseAllHandles>();

        #endregion

        #endregion
    }
}
