using SlideGenerator.Application.Modules.Registry.Entities;
using SlideGenerator.Application.Modules.Registry.Interfaces;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     Base class for presentation-related workflow steps.
///     Handles the acquisition and automatic disposal of presentation leases.
/// </summary>
/// <param name="presentationRegistry">The registry used to manage and acquire presentation instances.</param>
public abstract class PresentationStepBase(FileRegistry<IPresentation> presentationRegistry) : StepBodyAsync
{
    /// <summary>
    ///     Acquires a presentation for editing at the specified path.
    ///     The lease is automatically disposed of when the step finishes execution.
    /// </summary>
    /// <param name="outputPath">The path to the presentation file.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An <see cref="IPresentation" /> instance for editing.</returns>
    protected async ValueTask<IPresentation> AcquirePresentationAsync(string outputPath, CancellationToken ct)
    {
        // For WorkflowCore, we should ideally manage the lease lifetime carefully.
        // Since we are in a Step, we acquire it, use it, and then it's released when the lease is disposed of.
        // If we need to keep it open across steps, we'd need a different mechanism,
        // but typically WorkflowCore steps should be discrete.
        // However, given the original code's pattern, we'll acquire it per step for now.
        // The FileRegistry handles the actual locking.
        
        var lease = await presentationRegistry.AcquireAsync(Path.GetFullPath(outputPath), true, ct).ConfigureAwait(false);
        _contextLease = lease;
        return lease.Value;
    }

    /// <summary>
    ///     The lease for the presentation currently held by the context.
    /// </summary>
    private IAsyncDisposable? _contextLease;

    /// <summary>
    ///     Gets or sets the exception that occurred during step execution, if any.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    ///     Runs the step logic, ensuring the presentation lease is disposed of correctly.
    /// </summary>
    /// <param name="context">The execution context for the step.</param>
    /// <returns>An <see cref="ExecutionResult" /> indicating the outcome of the step.</returns>
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
            if (_contextLease != null)
            {
                await _contextLease.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    ///     Executes the core logic of the step.
    /// </summary>
    /// <param name="context">The execution context for the step.</param>
    /// <returns>An <see cref="ExecutionResult" /> indicating the outcome of the step.</returns>
    protected abstract Task<ExecutionResult> ExecuteStepAsync(IStepExecutionContext context);
}
