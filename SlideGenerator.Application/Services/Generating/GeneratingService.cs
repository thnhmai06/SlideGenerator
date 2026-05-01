using SlideGenerator.Application.Services.Generating.Models;
using SlideGenerator.Application.Services.Generating.Workflows;
using SlideGenerator.Application.Services.Scanning.Workflows;
using WorkflowCore.Interface;

namespace SlideGenerator.Application.Services.Generating;

/// <summary>
///     Implements <see cref="IGeneratingService" /> using the WorkflowCore engine for background execution and persistence.
/// </summary>
/// <param name="workflowHost">The host used to start and manage workflow instances.</param>
/// <param name="persistenceProvider">The provider used to retrieve workflow state and data.</param>
public sealed class GeneratingService(
    IWorkflowHost workflowHost,
    IPersistenceProvider persistenceProvider) : IGeneratingService
{
    /// <inheritdoc />
    public async Task<string> StartGenerationAsync(GeneratingRequest request, ScanningData? scanData = null)
    {
        var data = new GeneratingData { Request = request };
        
        if (scanData != null)
        {
            data.WorkbookSummaries = scanData.WorkbookSummaries;
            data.PresentationSummaries = scanData.PresentationSummaries;
        }

        return await workflowHost.StartWorkflow(nameof(GeneratingWorkflow), data).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<GeneratingData?> GetGenerationDataAsync(string workflowId)
    {
        var instance = await persistenceProvider.GetWorkflowInstance(workflowId).ConfigureAwait(false);
        return instance?.Data as GeneratingData;
    }

    /// <inheritdoc />
    public Task StopGenerationAsync(string workflowId)
    {
        return workflowHost.TerminateWorkflow(workflowId);
    }

    /// <inheritdoc />
    public Task PauseGenerationAsync(string workflowId)
    {
        return workflowHost.SuspendWorkflow(workflowId);
    }

    /// <inheritdoc />
    public Task ResumeGenerationAsync(string workflowId)
    {
        return workflowHost.ResumeWorkflow(workflowId);
    }
}
