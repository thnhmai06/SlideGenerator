using SlideGenerator.Slides.Services;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Workflows.Generating.Activities;

/// <summary>
///     Base class for presentation-related workflow steps.
/// </summary>
public abstract class PresentationStepBase(SfPresentationRegistry presentationRegistry) : StepBodyAsync
{
    private IDisposable? _contextLease;

    public Exception? Exception { get; set; }

    protected async ValueTask<SfPresentation> AcquirePresentationAsync(string outputPath, CancellationToken ct)
    {
        var lease = await presentationRegistry.AcquireAsync(Path.GetFullPath(outputPath), true, ct).ConfigureAwait(false);
        _contextLease = lease;
        return lease.Value;
    }

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        try
        {
            return await ExecuteStepAsync(context);
        }
        catch (Exception ex)
        {
            Exception = ex;
            return ExecutionResult.Next();
        }
        finally
        {
            _contextLease?.Dispose();
        }
    }

    protected abstract Task<ExecutionResult> ExecuteStepAsync(IStepExecutionContext context);
}