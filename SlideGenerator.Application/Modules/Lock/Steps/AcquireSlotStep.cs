using SlideGenerator.Application.Modules.Lock.Services;
using SlideGenerator.Application.Services.Generating.Rules;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Application.Modules.Lock.Steps;

/// <summary>
///     A workflow step that acquires a lock on a specified <see cref="Gate" />.
/// </summary>
/// <param name="gateLocker">The service used to manage gate locks.</param>
public sealed class AcquireSlotStep(GateLocker gateLocker) : StepBodyAsync
{
    /// <summary>
    ///     Gets the type of gate to acquire.
    /// </summary>
    public required GateType Gate { get; init; }

    /// <summary>
    ///     Executes the step by acquiring the lock.
    /// </summary>
    /// <param name="context">The execution context for the step.</param>
    /// <returns>An <see cref="ExecutionResult" /> indicating the outcome of the step.</returns>
    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        await gateLocker.AcquireAsync(Gate);
        return ExecutionResult.Next();
    }
}