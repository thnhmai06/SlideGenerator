using SlideGenerator.Application.Modules.Workflows.Abstractions;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Modules.Workflows.Models.States;
using WorkflowCore.Interface;

namespace SlideGenerator.Infrastructure.Workflows.Adapters;

public class WcWorkflowService(
    IServiceProvider services,
    IWorkflowHost workflowHost,
    IWorkflowController workflowController,
    WorkflowSnapshotRegistry snapshotRegistry) : IWorkflowService
{
    public async Task<string> RunAsync<TDef, TData>(TData data, CancellationToken ct = default)
        where TDef : Application.Modules.Workflows.DSL.IWorkflow<TData>, new()
        where TData : class, new()
    {
        var def = services.GetService(typeof(TDef)) as Application.Modules.Workflows.DSL.IWorkflow<TData>
                  ?? throw new InvalidOperationException($"Workflow definition {typeof(TDef).Name} is not registered.");

        // WorkflowCore's RegisterWorkflow<T> requires T: IWorkflow (non-generic alias for IWorkflow<object>),
        // which conflicts with IWorkflow<TData> at compile time. Use reflection to register at runtime.
        var registerMethod = workflowHost.GetType()
                                 .GetMethod("RegisterWorkflow")
                             ?? typeof(IWorkflowController)
                                 .GetMethod("RegisterWorkflow");
        registerMethod?
            .MakeGenericMethod(typeof(WcWorkflowAdapter<TDef, TData>))
            .Invoke(workflowHost, null);
        return await workflowHost.StartWorkflow(def.Id, def.Version, data).ConfigureAwait(false);
    }

    public Task PauseAsync(string id)
    {
        return workflowController.SuspendWorkflow(id);
    }

    public Task ResumeAsync(string id)
    {
        return workflowController.ResumeWorkflow(id);
    }

    public Task CancelAsync(string id)
    {
        return workflowController.TerminateWorkflow(id);
    }

    public IEnumerable<WorkflowSnapshot> Active => snapshotRegistry.All;

    public Task<WorkflowSnapshot?> GetWorkflow(string id)
    {
        snapshotRegistry.TryGet(id, out var state);
        return Task.FromResult(state);
    }
}