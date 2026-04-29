using System.Collections.Concurrent;
using SlideGenerator.Application.Modules.Workflows.Models.States;
using SlideGenerator.Infrastructure.Workflows.Adapters;

namespace SlideGenerator.Infrastructure.Workflows;

/// <summary>
///     Singleton store that tracks active <see cref="WorkflowSnapshot" /> instances by workflow instance ID.
///     <see cref="WcInterpreterStep{TDef,TData}" /> registers a snapshot when execution begins and removes
///     it on completion; <see cref="WcWorkflowService" /> reads from this registry to expose running workflows.
/// </summary>
public sealed class WorkflowSnapshotRegistry
{
    private readonly ConcurrentDictionary<string, WorkflowSnapshot> _snapshots = new();

    /// <summary>Returns all currently active workflow snapshots.</summary>
    public IEnumerable<WorkflowSnapshot> All => _snapshots.Values;

    /// <summary>Looks up a workflow snapshot by instance ID.</summary>
    public bool TryGet(string id, out WorkflowSnapshot? snapshot) =>
        _snapshots.TryGetValue(id, out snapshot);

    internal void Register(string id, WorkflowSnapshot snapshot) => _snapshots[id] = snapshot;

    internal void Unregister(string id) => _snapshots.TryRemove(id, out _);
}
