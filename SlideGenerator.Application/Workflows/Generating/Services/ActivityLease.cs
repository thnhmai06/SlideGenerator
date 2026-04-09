using SlideGenerator.Application.Resources;

namespace SlideGenerator.Application.Workflows.Generating.Services;

/// <summary>
///     Represents an acquired activity execution permit in the global gate.
/// </summary>
public sealed class ActivityLease(ActivityGate gate)
    : Lease
{
    /// <inheritdoc />
    protected override void Release()
    {
        gate.Release();
    }
}