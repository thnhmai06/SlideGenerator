using System.Collections.Concurrent;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Domain.Cloud.Abstractions;
using SlideGenerator.Domain.Sheet.Entities;
using SlideGenerator.Domain.Sheet.Models;
using SlideGenerator.Domain.Tasks.Models.Image;

namespace SlideGenerator.Application.Tasks.Activities;

/// <summary>
///     Workflow step that resolves source URLs for image instructions.
/// </summary>
public sealed class ResolveImageUrls(
    ICloudResolver cloudResolver,
    IRegistry<IReadOnlyWorkbook> workbookRegistry) : Activity
{
    /// <summary>Input: Image replacement instructions specialized for the current row and slide.</summary>
    public Input<IReadOnlyList<SpecializedInstruction>> ImageInstructions { get; set; } = null!;

    /// <summary>Input: 1-based row index in the target worksheet.</summary>
    public Input<int> RowIndex { get; set; } = new(1);

    /// <summary>Input: Target worksheet identifier used to load row content.</summary>
    public Input<WorksheetIdentifier> WorksheetInfo { get; set; } = null!;

    /// <summary>
    ///     Output resolved URLs keyed by specialized image instruction.
    /// </summary>
    public Output<IReadOnlyDictionary<SpecializedInstruction, string>> ResolvedImageUrls { get; set; } = null!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var worksheetInfo = context.Get(WorksheetInfo);
        var rowIndex = context.Get(RowIndex);

        if (worksheetInfo is null || rowIndex <= 0)
            throw new ArgumentException("Worksheet identifier and row index must be valid.");

        var workbook = workbookRegistry.GetOrOpen(worksheetInfo.Workbook.FilePath, isEditable: false);
        var rowContent = !workbook.TryGetWorksheet(worksheetInfo.Name, out var worksheet)
            ? throw new InvalidOperationException($"Worksheet '{worksheetInfo.Name}' does not exist in workbook.")
            : new Dictionary<string, string>(worksheet.GetRowContent(rowIndex), StringComparer.Ordinal);


        var imageInstructions = context.Get(ImageInstructions) ?? [];
        var resolvedUrls = new ConcurrentDictionary<SpecializedInstruction, string>();

        foreach (var imageInstruction in imageInstructions)
        {
            if (!rowContent.TryGetValue(imageInstruction.Source.ColumnName, out var sourceValue) ||
                string.IsNullOrWhiteSpace(sourceValue))
                continue;
            if (!Uri.TryCreate(sourceValue, UriKind.Absolute, out var sourceUri))
                continue;

            var resolved = cloudResolver.IsUriSupported(sourceUri)
                ? await cloudResolver.ResolveUriAsync(sourceUri).ConfigureAwait(false)
                : sourceUri;

            resolvedUrls.TryAdd(imageInstruction, resolved.ToString());
        }

        context.Set(ResolvedImageUrls,
            new Dictionary<SpecializedInstruction, string>(resolvedUrls));
    }
}