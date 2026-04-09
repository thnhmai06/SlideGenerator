using SlideGenerator.Domain.Settings.Entities;
using SlideGenerator.Domain.Settings.Interfaces;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Acquires one global Preparing-flow activity permit.
/// </summary>
public sealed class AcquirePreparingSlot(ISettingProvider settingProvider)
    : AcquireActivitySlot(settingProvider)
{
    /// <inheritdoc />
    protected override string GateKey => "Generation.PreparingGate";

    /// <inheritdoc />
    protected override string LeaseKey => "Generation.PreparingLease";

    /// <inheritdoc />
    protected override int GetConfiguredConcurrency(Setting setting)
    {
        return setting.Job.MaxConcurrentPreparingFlows;
    }
}
