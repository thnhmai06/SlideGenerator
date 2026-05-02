using SlideGenerator.Workflows.Scanning;
using SlideGenerator.Workflows.Scanning.Models;
using WorkflowCore.Interface;

namespace SlideGenerator.Workflows.Services;

public sealed class ScanningService(
    IWorkflowHost workflowHost,
    IPersistenceProvider persistenceProvider)
{
    public async Task<string> StartScanAsync(ScanningRequest request)
    {
        var data = new ScanningData { Request = request };
        return await workflowHost.StartWorkflow(nameof(ScanningWorkflow), data).ConfigureAwait(false);
    }

    public async Task<ScanningData?> GetScanDataAsync(string workflowId)
    {
        var instance = await persistenceProvider.GetWorkflowInstance(workflowId).ConfigureAwait(false);
        return instance?.Data as ScanningData;
    }
}