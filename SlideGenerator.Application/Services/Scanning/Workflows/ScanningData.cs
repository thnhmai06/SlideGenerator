using System.Collections.Concurrent;
using SlideGenerator.Application.Services.Scanning.Models;
using SlideGenerator.Application.Services.Scanning.Models.Sheets;
using SlideGenerator.Application.Services.Scanning.Models.Slides;

namespace SlideGenerator.Application.Services.Scanning.Workflows;

/// <summary>
///     Represents the internal state and results of a scanning workflow instance.
///     This data is persisted by WorkflowCore during the execution of the <see cref="ScanningWorkflow" />.
/// </summary>
public sealed class ScanningData
{
    /// <summary>
    ///     Gets or sets the initial request containing the files to be scanned.
    /// </summary>
    public ScanningRequest Request { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the results of workbook scans, keyed by the normalized absolute file path.
    /// </summary>
    public ConcurrentDictionary<string, WorkbookSummary> WorkbookSummaries { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Gets or sets the results of presentation scans, keyed by the normalized absolute file path.
    /// </summary>
    public ConcurrentDictionary<string, PresentationSummary> PresentationSummaries { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Gets or sets any errors encountered during the scan, keyed by the normalized absolute file path.
    /// </summary>
    public ConcurrentDictionary<string, Exception> Errors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
