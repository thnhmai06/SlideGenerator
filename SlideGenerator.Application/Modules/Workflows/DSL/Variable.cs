namespace SlideGenerator.Application.Modules.Workflows.DSL;

/// <summary>
///     A strongly typed identifier for a workflow-scoped variable.
///     Instances carry no data — they serve only as typed keys into the
///     <see cref="IActivityContext" /> scope dictionary.
///     Because <see cref="Variable{T}" /> is stateless, its instances are safe to declare
///     as <c>public static readonly</c> fields.
/// </summary>
/// <typeparam name="T">The value type this variable represents.</typeparam>
/// <param name="Name">The unique name used as the lookup key in the scope chain.</param>
public sealed record Variable<T>(string Name);