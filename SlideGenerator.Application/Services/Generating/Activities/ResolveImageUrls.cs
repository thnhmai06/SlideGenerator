using System.Collections.Concurrent;
using SlideGenerator.Application.Cloud.Abstractions;
using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;
using SpecializedInstruction = SlideGenerator.Application.Services.Generating.Models.Images.SpecializedInstruction;

namespace SlideGenerator.Application.Services.Generating.Activities;

/// <summary>
///     Resolves source URLs for image instructions by reading the specified row from the worksheet
///     and running each raw URL through the cloud resolver chain.
/// </summary>
public sealed class ResolveImageUrls(
    ICloudResolver cloudResolver,
    FileRegistry<IReadOnlyWorkbook> workbookRegistry) : Activity
{
    /// <inheritdoc />
    public override async ValueTask ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        var worksheetInfo = context.GetVariable<WorksheetIdentifier>(WorksheetContextRules.Worksheet)!;

        // In the original, this was an init property. However, it seems it should read the first row
        // as a default when preparing, or perhaps we don't need RowIndex here if we are just resolving
        // for row 1 during preparation phase to find all URLs?
        // Wait, looking at the previous file content, RowIndex was hardcoded to 1. I'll leave it as 1 for now.
        int rowIndex = 1;

        using var workbookLease = await workbookRegistry
            .AcquireAsync(worksheetInfo.Workbook.FilePath, false, cancellationToken)
            .ConfigureAwait(false);
        var workbook = workbookLease.Value;
        var rowContent = !workbook.TryGetWorksheet(worksheetInfo.Name, out var worksheet)
            ? throw new InvalidOperationException($"Worksheet '{worksheetInfo.Name}' does not exist in workbook.")
            : new Dictionary<string, string>(worksheet.GetRowContent(rowIndex), StringComparer.Ordinal);

        var imageInstructions = context.GetVariable<IReadOnlyList<SpecializedInstruction>>(WorksheetContextRules.ImageInstructions) ?? [];
        var resolvedUrls = new ConcurrentDictionary<SpecializedInstruction, string>();

        foreach (var imageInstruction in imageInstructions)
        {
            if (!rowContent.TryGetValue(imageInstruction.Source.Name, out var sourceValue) ||
                string.IsNullOrWhiteSpace(sourceValue))
                continue;
            if (!Uri.TryCreate(sourceValue, UriKind.Absolute, out var sourceUri))
                continue;

            var resolved = cloudResolver.TryIsUriSupported(sourceUri, out _)
                ? await cloudResolver.ResolveUriAsync(sourceUri, cancellationToken).ConfigureAwait(false)
                : sourceUri;

            resolvedUrls.TryAdd(imageInstruction, resolved.ToString());
        }

        context.SetVariable<IReadOnlyDictionary<SpecializedInstruction, string>>(WorksheetContextRules.ResolvedImageUrls, new Dictionary<SpecializedInstruction, string>(resolvedUrls));
    }
}
