namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Releases the global Preparing-flow activity permit acquired for the current workflow branch.
/// </summary>
public sealed class ReleasePreparingSlot : ReleaseActivitySlot
{
    /// <inheritdoc />
    protected override string LeaseKey => "Generation.PreparingLease";
}