namespace SlideGenerator.Coordinator.Domain.Models;

/// <summary>
/// Represents a participation model in the workflow coordination process.
/// Determines the role (primary or secondary) of a participant in handling
/// operations for a specific workflow key.
/// </summary>
public abstract record Enlistment;

/// <summary>
///     Returned to the first caller: this step owns the operation.
///     Call <see cref="SubmitResult" /> with the output path (or <c>null</c> on failure) when done.
/// </summary>
public sealed record PrimaryEnlistment(Action<string?> SubmitResult) : Enlistment;

/// <summary>
///     Returned to every subsequent caller: await <see cref="WaitTask" /> to get
///     the primary's output path (<c>null</c> if the primary failed).
/// </summary>
public sealed record SecondaryEnlistment(Task<string?> WaitTask) : Enlistment;
