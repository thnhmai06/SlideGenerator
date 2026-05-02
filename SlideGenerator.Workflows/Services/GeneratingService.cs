using SlideGenerator.Workflows.Generating;
using SlideGenerator.Workflows.Generating.Models;
using SlideGenerator.Workflows.Scanning;
using WorkflowCore.Interface;

namespace SlideGenerator.Workflows.Services;

public sealed class GeneratingService(
    IWorkflowHost workflowHost,
    IPersistenceProvider persistenceProvider)
{
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

    public async Task<GeneratingData?> GetGenerationDataAsync(string workflowId)
    {
        var instance = await persistenceProvider.GetWorkflowInstance(workflowId).ConfigureAwait(false);
        return instance?.Data as GeneratingData;
    }

    public Task StopGenerationAsync(string workflowId) => workflowHost.TerminateWorkflow(workflowId);
    public Task PauseGenerationAsync(string workflowId) => workflowHost.SuspendWorkflow(workflowId);
    public Task ResumeGenerationAsync(string workflowId) => workflowHost.ResumeWorkflow(workflowId);
}