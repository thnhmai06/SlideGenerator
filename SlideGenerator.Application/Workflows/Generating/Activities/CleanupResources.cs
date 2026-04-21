using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Disposes long-lived registry leases stored in
///     <see cref="WorkflowExecutionContext.TransientProperties" /> by
///     <see cref="CreateWorkingPresentation" />.
/// </summary>
public sealed class CleanupResources : Activity
{
    /// <summary>Optional collection of presentation identifiers whose leases should be released.</summary>
    public Input<IReadOnlySet<PresentationIdentifier>>? Presentations { get; init; }

    /// <summary>Optional collection of workbook identifiers (reserved for future use).</summary>
    public Input<IReadOnlySet<WorkbookIdentifier>>? Workbooks { get; init; }

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var presentations = Presentations is null ? null : context.Get(Presentations);
        if (presentations is not null)
            foreach (var identifier in presentations)
            {
                var key = CreateWorkingPresentation.GetLeaseKey(Path.GetFullPath(identifier.FilePath));
                if (context.WorkflowExecutionContext.TransientProperties.TryGetValue(key, out var v) &&
                    v is IDisposable lease)
                {
                    lease.Dispose();
                    context.WorkflowExecutionContext.TransientProperties.Remove(key);
                }
            }

        return ValueTask.CompletedTask;
    }
}
