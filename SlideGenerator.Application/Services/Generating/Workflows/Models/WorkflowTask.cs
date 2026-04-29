using SlideGenerator.Application.Services.Generating.Models;

namespace SlideGenerator.Application.Services.Generating.Workflows.Models;

/// <summary>
///     Persisted workflow data for a <see cref="GeneratingWorkflow" /> run.
///     Contains only the original input request; all computed state is stored in
///     <c>Variable&lt;T&gt;</c> scope chain entries
///     so that restoring Variables from a checkpoint is enough to resume execution.
/// </summary>
public class WorkflowTask
{
    /// <summary>The original generation request supplying the graph, instructions, and output settings.</summary>
    public GeneratingRequest Request { get; init; } = null!;
}