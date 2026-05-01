using SlideGenerator.Application.Services.Scanning.Models;
using SlideGenerator.Application.Services.Scanning.Workflows;
using WorkflowCore.Interface;

namespace SlideGenerator.Application.Services.Scanning;

/// <summary>
///     Implements <see cref="IScanningService" /> using the WorkflowCore engine for background execution and persistence.
/// </summary>
/// <param name="workflowHost">The host used to start and manage workflow instances.</param>
/// <param name="persistenceProvider">The provider used to retrieve workflow state and data.</param>
public sealed class ScanningService(
    IWorkflowHost workflowHost,
    IPersistenceProvider persistenceProvider) : IScanningService
{
    /// <inheritdoc />
    public async Task<string> StartScanAsync(ScanningRequest request)
    {
        var data = new ScanningData { Request = request };
        return await workflowHost.StartWorkflow(nameof(ScanningWorkflow), data).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ScanningData?> GetScanDataAsync(string workflowId)
    {
        var instance = await persistenceProvider.GetWorkflowInstance(workflowId).ConfigureAwait(false);
        return instance?.Data as ScanningData;
    }
}
