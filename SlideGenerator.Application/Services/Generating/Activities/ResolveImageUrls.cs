using SlideGenerator.Application.Cloud.Abstractions;
using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Services.Generating.Models.Images;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Services.Generating.Services;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Application.Services.Generating.Activities;

/// <summary>
///     Resolves source URLs for images for a specific row using the instruction resolver.
/// </summary>
/// <param name="cloudResolver">The cloud URL resolver.</param>
/// <param name="workbookRegistry">The workbook file registry.</param>
public sealed class ResolveImageUrls(ICloudResolver cloudResolver, FileRegistry<IReadOnlyWorkbook> workbookRegistry)
    : Activity
{
    /// <inheritdoc />
    public override async ValueTask ExecuteAsync(IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var worksheetInfo = context.GetVariable<WorksheetIdentifier>(WorksheetContextRules.Worksheet)!;

        // Note: For the preparation phase, we still use row 1.
        const int rowIndex = 1;

        using var workbookLease = await workbookRegistry
            .AcquireAsync(worksheetInfo.Workbook.FilePath, false, cancellationToken)
            .ConfigureAwait(false);
        var workbook = workbookLease.Value;
        if (!workbook.TryGetWorksheet(worksheetInfo.Name, out var worksheet))
            throw new InvalidOperationException($"Worksheet '{worksheetInfo.Name}' does not exist in workbook.");

        var rowContent = worksheet.GetRowContent(rowIndex);

        var generalInstructions =
            context.GetVariable<IReadOnlyList<GeneralInstruction>>(WorksheetContextRules.ImageInstructions) ?? [];

        var specializedInstructions = new List<SpecializedInstruction>();

        foreach (var general in generalInstructions)
            if (InstructionResolver.TryResolveImage(general, rowContent, out var specialized))
            {
                // Optionally resolve cloud URIs here if not already done in NormalizeUri
                if (specialized.Value != null && cloudResolver.TryIsUriSupported(specialized.Value, out _))
                {
                    var resolvedUri = await cloudResolver.ResolveUriAsync(specialized.Value, cancellationToken)
                        .ConfigureAwait(false);
                    specialized = specialized with { Value = resolvedUri };
                }

                specializedInstructions.Add(specialized);
            }
            else
            {
                // Fallback: add a null-value instruction if no source matched
                specializedInstructions.Add(new SpecializedInstruction(general.Target, null, general.Edit));
            }

        context.SetVariable<IReadOnlyList<SpecializedInstruction>>(
            WorksheetContextRules.ResolvedImageUrls, specializedInstructions);
    }
}