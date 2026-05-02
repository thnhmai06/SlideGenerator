using System.Collections.Concurrent;
using SlideGenerator.Workflows.Scanning.Models;

namespace SlideGenerator.Workflows.Scanning;

public sealed class ScanningData
{
    public ScanningRequest Request { get; set; } = null!;
    public ConcurrentDictionary<string, WorkbookSummary> WorkbookSummaries { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public ConcurrentDictionary<string, PresentationSummary> PresentationSummaries { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public ConcurrentDictionary<string, Exception> Errors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}