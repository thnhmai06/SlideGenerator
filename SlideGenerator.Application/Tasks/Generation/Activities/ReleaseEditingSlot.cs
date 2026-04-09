namespace SlideGenerator.Application.Tasks.Generation.Activities;

/// <summary>
///     Releases the global Editing-flow activity permit acquired for the current workflow branch.
/// </summary>
public sealed class ReleaseEditingSlot : ReleaseActivitySlot
{
    /// <inheritdoc />
    protected override string LeaseKey => "Generation.EditingLease";
}
