using SlideGenerator.Application.Services.Generating.Models;
using SlideGenerator.Application.Services.Generating.Workflows;
using SlideGenerator.Application.Services.Scanning.Workflows;

namespace SlideGenerator.Application.Services.Generating;

/// <summary>
///     Provides high-level operations for generating presentations via workflows.
/// </summary>
public interface IGeneratingService
{
    /// <summary>
    ///     Starts a generation workflow for the given request and pre-scanned data.
    /// </summary>
    /// <param name="request">The generation request.</param>
    /// <param name="scanData">Optional pre-scanned data to avoid redundant scans.</param>
    /// <returns>The unique identifier of the started workflow instance.</returns>
    Task<string> StartGenerationAsync(GeneratingRequest request, ScanningData? scanData = null);

    /// <summary>
    ///     Gets the current state of a generation workflow instance.
    /// </summary>
    /// <param name="workflowId">The identifier of the workflow instance.</param>
    /// <returns>The generation data, or null if not found.</returns>
    Task<GeneratingData?> GetGenerationDataAsync(string workflowId);

    /// <summary>
    ///     Stops (terminates) a running generation workflow.
    /// </summary>
    /// <param name="workflowId">The identifier of the workflow instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopGenerationAsync(string workflowId);

    /// <summary>
    ///     Pauses (suspends) a running generation workflow.
    /// </summary>
    /// <param name="workflowId">The identifier of the workflow instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PauseGenerationAsync(string workflowId);

    /// <summary>
    ///     Resumes a previously paused generation workflow.
    /// </summary>
    /// <param name="workflowId">The identifier of the workflow instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ResumeGenerationAsync(string workflowId);
}
