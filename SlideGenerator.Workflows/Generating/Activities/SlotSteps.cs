using SlideGenerator.Gate.Models;
using SlideGenerator.Gate.Services;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Workflows.Generating.Activities;

public sealed class AcquireSlotStep(GateLocker gateLocker) : StepBodyAsync
{
    public required GateType Gate { get; init; }

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        await gateLocker.AcquireAsync(Gate);
        return ExecutionResult.Next();
    }
}

public sealed class ReleaseSlotStep(GateLocker gateLocker) : StepBody
{
    public required GateType Gate { get; init; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        gateLocker.Release(Gate);
        return ExecutionResult.Next();
    }
}