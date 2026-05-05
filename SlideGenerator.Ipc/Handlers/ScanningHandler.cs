using SlideGenerator.Documents.Sheets.Models;
using SlideGenerator.Documents.Slides.Models;
using SlideGenerator.Pipelines.Scanning;
using SlideGenerator.Pipelines.Scanning.Models.Sheets.Requests;
using SlideGenerator.Pipelines.Scanning.Models.Sheets.Responses;
using SlideGenerator.Pipelines.Scanning.Models.Slides.Requests;
using SlideGenerator.Pipelines.Scanning.Models.Slides.Responses;

namespace SlideGenerator.Ipc.Handlers;

/// <summary>
///     Handles all <c>scanning.*</c> JSON-RPC methods for inspecting Excel workbooks
///     and PowerPoint presentations before a generation job is started.
/// </summary>
public sealed class ScanningHandler(ScanningService scanningService)
{
    /// <summary>
    ///     Scans an Excel workbook and returns its structure, including worksheet names,
    ///     row counts, and an optional data preview.
    /// </summary>
    /// <param name="request">
    ///     The scan request deserialized directly from the JSON-RPC payload,
    ///     containing a <see cref="BookIdentifier" /> and a preview flag.
    /// </param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>
    ///     A <see cref="WorkbookSummary" /> describing the workbook structure.
    /// </returns>
    public Task<WorkbookSummary> ScanWorkbookAsync(BookSummaryRequest request, CancellationToken ct)
    {
        return scanningService.ScanWorkbookAsync(request);
    }

    /// <summary>
    ///     Scans a PowerPoint presentation and returns its structure, including slide placeholders,
    ///     image shape names and bounds, and optional slide thumbnail previews.
    /// </summary>
    /// <param name="request">
    ///     The scan request deserialized directly from the JSON-RPC payload,
    ///     containing a <see cref="PresentationIdentifier" /> and a preview flag.
    /// </param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>
    ///     A <see cref="PresentationSummary" /> describing the presentation structure.
    /// </returns>
    public Task<PresentationSummary> ScanPresentationAsync(PresentationSummaryRequest request, CancellationToken ct)
    {
        return scanningService.ScanPresentationAsync(request);
    }
}