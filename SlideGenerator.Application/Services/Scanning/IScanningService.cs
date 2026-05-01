using SlideGenerator.Application.Services.Scanning.Models;
using SlideGenerator.Application.Services.Scanning.Workflows;

namespace SlideGenerator.Application.Services.Scanning;

/// <summary>
///     Provides high-level operations for scanning workbooks and presentations using background workflows.
/// </summary>
public interface IScanningService
{
    /// <summary>
    ///     Starts a scanning workflow for the given request.
    /// </summary>
    /// <param name="request">The scanning request containing file identifiers for workbooks and presentations.</param>
    /// <returns>The unique identifier of the started workflow instance.</returns>
    /// <remarks>
    ///     The scanning process runs asynchronously in the background. Use the returned ID to track status or retrieve results.
    /// </remarks>
    Task<string> StartScanAsync(ScanningRequest request);

    /// <summary>
    ///     Gets the data collected by a specific scanning workflow instance.
    /// </summary>
    /// <param name="workflowId">The unique identifier of the workflow instance.</param>
    /// <returns>
    ///     A <see cref="ScanningData" /> object containing the gathered workbook and presentation summaries, 
    ///     or <see langword="null" /> if no workflow with the specified identifier was found.
    /// </returns>
    Task<ScanningData?> GetScanDataAsync(string workflowId);
}
