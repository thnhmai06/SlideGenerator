using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Resources;
using SlideGenerator.Application.Systems.Abstractions;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using SlideGenerator.Domain.Slides.Rules;

namespace SlideGenerator.Application.Workflows.Generating.Activities;

/// <summary>
///     Workflow step that prepares an output presentation from a template and keeps it opened for downstream processing.
/// </summary>
/// <remarks>
///     <para>
///         The workflow runs one inline activity:
///     </para>
///     <list type="number">
///         <item>
///             <description>
///                 <c>PreparePresentation</c> loads the template into memory, keeps only <see cref="TemplateSlide" />,
///                 converts to <see cref="PresentationExtension" />, writes the result to <see cref="OutputPath" />,
///                 and opens the cloned output via presentation registry.
///             </description>
///         </item>
///     </list>
/// </remarks>
public sealed class CreateWorkingPresentation(
    Registry<IPresentation> slideRegistry,
    Registry<IReadOnlyWorkbook> workbookRegistry,
    IFileSystem fileSystem) : Activity
{
    /// <summary>
    ///     Input: File path and index of the template slide to use.
    /// </summary>
    public required Input<SlideIdentifier> TemplateSlide { get; init; }

    /// <summary>
    ///     Input: Target worksheet that must exist in the source workbook before creating output presentation.
    /// </summary>
    public required Input<WorksheetIdentifier> Worksheet { get; init; }

    /// <summary>
    ///     Input: Full output path for the generated presentation file.
    /// </summary>
    public required Input<string> OutputPath { get; init; }

    /// <summary>
    ///     Output: The only template slide kept in the working presentation.
    /// </summary>
    public Output<SlideIdentifier> WorkingTemplateSlide { get; init; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var templateSlideIdentifier = context.Get(TemplateSlide);
        if (templateSlideIdentifier is null)
            throw new ArgumentException("Invalid template slide provided.", nameof(TemplateSlide));

        var outputPath = context.Get(OutputPath);
        var worksheet = context.Get(Worksheet);

        if (string.IsNullOrWhiteSpace(outputPath) || worksheet is null)
            throw new ArgumentException("Invalid output path or worksheet information provided.", nameof(OutputPath));

        var workbookPath = Path.GetFullPath(worksheet.Workbook.FilePath);
        if (!File.Exists(workbookPath))
            throw new FileNotFoundException("Workbook file not found.", workbookPath);

        using var workbookLease = workbookRegistry.Acquire(workbookPath, true);
        var workbook = workbookLease.Value;
        if (!workbook.TryGetWorksheet(worksheet.Name, out _))
            throw new InvalidOperationException(
                $"Worksheet '{worksheet.Name}' does not exist in workbook '{workbook.Name}'.");

        var templatePath = Path.GetFullPath(templateSlideIdentifier.Presentation.FilePath);
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("Template presentation file not found.", templatePath);
        
        fileSystem.CopyFile(templatePath, outputPath, overwrite: true);

        var workingPresentation = slideRegistry.GetOrOpen(outputPath, true);
        var totalSlides = workingPresentation.EnumerateSlides().Count();
        for (var index = totalSlides; index >= 1; index--)
        {
            if (index != templateSlideIdentifier.Index)
                workingPresentation.RemoveSlide(index);
        }
        var outputExtensionType = PresentationExtensions.FromFileExtension(Path.GetExtension(outputPath));
        workingPresentation.Save(outputExtensionType);
        
        context.Set(WorkingTemplateSlide, new PresentationIdentifier(outputPath).GetSlide(1));
        return ValueTask.CompletedTask;
    }
}