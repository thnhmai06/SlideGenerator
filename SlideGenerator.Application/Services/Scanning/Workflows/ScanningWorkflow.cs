using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Modules.Workflows.DSL.Activities;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Services.Scanning.Models;
using SlideGenerator.Application.Services.Scanning.Models.Sheets;
using SlideGenerator.Application.Services.Scanning.Models.Slides;
using SlideGenerator.Application.Services.Scanning.Workflows.Activities;
using SlideGenerator.Domain.Sheets.Models.Identifiers;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Scanning.Workflows;

/// <summary>
///     Defines the standalone scanning workflow for <see cref="ScanningRequest" />.
/// </summary>
public sealed class ScanningWorkflow(IServiceProvider services) : IWorkflow<ScanningRequest>
{
    private readonly GateLocker<GateType> _gateLocker = services.GetRequiredService<GateLocker<GateType>>();

    public ScanningWorkflow() : this(default!) { }

    /// <inheritdoc />
    public string Id => nameof(ScanningWorkflow);

    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public Activity<ScanningRequest> Build()
    {
        return new Sequence<ScanningRequest>
        {
            Steps =
            [
                // 0. Initialize scanning Variables if not already present in the context
                new Inline<ScanningRequest>
                {
                    Action = ctx =>
                    {
                        if (!ctx.TryGetVariable(ScanningVariables.WorkbookSummaries, out _))
                        {
                            ctx.SetVariable(ScanningVariables.WorkbookSummaries,
                                new ConcurrentDictionary<string, WorkbookSummary>(StringComparer.OrdinalIgnoreCase));
                        }

                        if (!ctx.TryGetVariable(ScanningVariables.PresentationSummaries, out _))
                        {
                            ctx.SetVariable(ScanningVariables.PresentationSummaries,
                                new ConcurrentDictionary<string, PresentationSummary>(StringComparer.OrdinalIgnoreCase));
                        }

                        return Task.CompletedTask;
                    }
                },

                // 1. Parallel initial scans
                new Parallel<ScanningRequest>
                {
                    Branches =
                    [
                        new ForEach<WorkbookIdentifier, ScanningRequest>(true)
                        {
                            Items = ctx => ctx.Data.Workbooks,
                            Handle = ScanningVariables.WorkbookItem,
                            Body = new GateWrapper<GateType, ScanningRequest>(_gateLocker)
                            {
                                Gate = GateType.ScanWorkbook,
                                Body = Inline<ScanningRequest>.Activity<ScanWorkbook>()
                            }
                        },
                        new ForEach<PresentationIdentifier, ScanningRequest>(true)
                        {
                            Items = ctx => ctx.Data.Presentations,
                            Handle = ScanningVariables.PresentationItem,
                            Body = new GateWrapper<GateType, ScanningRequest>(_gateLocker)
                            {
                                Gate = GateType.ScanPresentation,
                                Body = Inline<ScanningRequest>.Activity<ScanPresentation>()
                            }
                        }
                    ]
                }
            ]
        };
    }
}
