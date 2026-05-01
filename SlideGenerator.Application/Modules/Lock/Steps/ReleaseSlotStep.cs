using SlideGenerator.Application.Modules.Lock.Services;
using SlideGenerator.Application.Services.Generating.Rules;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Application.Modules.Lock.Steps;

/// <summary>
///     A workflow step that releases a lock on a specified <see cref="Gate" />.
/// </summary>
/// <param name="gateLocker">The service used to manage gate locks.</param>
public sealed class ReleaseSlotStep(GateLocker gateLocker) : StepBody
{
    /// <summary>
    ///     Gets the type of gate to release.
    /// </summary>
    public required GateType Gate { get; init; }

    /// <summary>
    ///     Executes the step by releasing the lock.
    /// </summary>
    /// <param name="context">The execution context for the step.</param>
    /// <returns>An <see cref="ExecutionResult" /> indicating the outcome of the step.</returns>
    public override ExecutionResult Run(IStepExecutionContext context)
    {
        gateLocker.Release(Gate);
        return ExecutionResult.Next();
    }
}