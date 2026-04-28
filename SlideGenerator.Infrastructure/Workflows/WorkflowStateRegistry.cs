using System.Collections.Concurrent;
using SlideGenerator.Application.Workflows.Models.States;

namespace SlideGenerator.Infrastructure.Workflows;

/// <summary>
///     Singleton store that tracks active <see cref="WorkflowState" /> instances by workflow instance ID.
///     <see cref="WcInterpreterStep{TDef,TData}" /> registers a state when execution begins and removes
///     it on completion; <see cref="WcWorkflowService" /> reads from this registry to expose running workflows.
/// </summary>
public sealed class WorkflowStateRegistry
{
    private readonly ConcurrentDictionary<string, WorkflowState> _states = new();

    /// <summary>Returns all currently active workflow states.</summary>
    public IEnumerable<WorkflowState> All => _states.Values;

    /// <summary>Looks up a workflow state by instance ID.</summary>
    public bool TryGet(string id, out WorkflowState? state) => _states.TryGetValue(id, out state);

    internal void Register(string id, WorkflowState state) => _states[id] = state;

    internal void Unregister(string id) => _states.TryRemove(id, out _);
}
