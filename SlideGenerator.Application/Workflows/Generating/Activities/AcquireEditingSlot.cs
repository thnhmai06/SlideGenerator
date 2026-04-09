using SlideGenerator.Domain.Settings.Entities;
using SlideGenerator.Domain.Settings.Interfaces;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Acquires one global Editing-flow activity permit.
/// </summary>
public sealed class AcquireEditingSlot(ISettingProvider settingProvider)
    : AcquireActivitySlot(settingProvider)
{
    /// <inheritdoc />
    protected override string GateKey => "Generation.EditingGate";

    /// <inheritdoc />
    protected override string LeaseKey => "Generation.EditingLease";

    /// <inheritdoc />
    protected override int GetConfiguredConcurrency(Setting setting)
    {
        return setting.Job.MaxConcurrentEditingFlows;
    }
}
