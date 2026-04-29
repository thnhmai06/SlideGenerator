namespace SlideGenerator.Application.Modules.Workflows.Models.States;

/// <summary>
///     Interfaces for transient execution-scoped state.
///     Derived classes add fields for resources that must not be persisted across restarts
///     (e.g., open file leases, in-flight locks).
///     Each <see cref="SlideGenerator.Application.Modules.Workflows.Models.States.ExecutionSnapshot" /> optionally carries one instance.
/// </summary>
public interface IExecutionContext;