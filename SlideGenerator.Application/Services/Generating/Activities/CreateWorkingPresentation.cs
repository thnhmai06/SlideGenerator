using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Systems.Abstractions;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using SlideGenerator.Domain.Slides.Rules;

namespace SlideGenerator.Application.Services.Generating.Activities;

/// <summary>Creates a working copy of a presentation.</summary>
/// <remarks>
///     Copies the template, strips it to a single slide, and manages the registry lease.
///     The caller is responsible for disposing the lease stored in <see cref="WorksheetContextRules.PresentationLease" />.
/// </remarks>
/// <param name="slideRegistry">The presentation file registry.</param>
/// <param name="workbookRegistry">The workbook file registry.</param>
/// <param name="fileSystem">The file system abstraction.</param>
/// <param name="graph">The worksheet-to-slide mapping.</param>
public sealed class CreateWorkingPresentation(
    FileRegistry<IPresentation> slideRegistry,
    FileRegistry<IReadOnlyWorkbook> workbookRegistry,
    IFileSystem fileSystem,
    IReadOnlyDictionary<WorksheetIdentifier, SlideIdentifier> graph) : Activity
{
    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown if template slide or output path is invalid.</exception>
    /// <exception cref="FileNotFoundException">Thrown if workbook or template file is missing.</exception>
    /// <exception cref="InvalidOperationException">Thrown if worksheet does not exist.</exception>
    public override async ValueTask ExecuteAsync(IExecutionContext context, CancellationToken cancellationToken = default)
    {
        var worksheet = context.GetVariable<WorksheetIdentifier>(WorksheetContextRules.Worksheet)!;
        var templateSlideIdentifier = graph[worksheet];
        var outputPath = context.GetVariable<string>(WorksheetContextRules.OutputPath);

        if (templateSlideIdentifier is null)
            throw new ArgumentException("Invalid template slide provided.");

        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path must be set before creating the working presentation.");

        var workbookPath = Path.GetFullPath(worksheet.Workbook.FilePath);
        if (!File.Exists(workbookPath))
            throw new FileNotFoundException("Workbook file not found.", workbookPath);

        using var workbookLease = await workbookRegistry
            .AcquireAsync(workbookPath, false, cancellationToken)
            .ConfigureAwait(false);

        var workbook = workbookLease.Value;
        if (!workbook.TryGetWorksheet(worksheet.Name, out _))
            throw new InvalidOperationException(
                $"Worksheet '{worksheet.Name}' does not exist in workbook '{workbook.Name}'.");

        var templatePath = Path.GetFullPath(templateSlideIdentifier.Presentation.FilePath);
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("Template presentation file not found.", templatePath);

        fileSystem.CopyFile(templatePath, outputPath);

        var normalizedOutput = Path.GetFullPath(outputPath);
        var presentationLease = await slideRegistry
            .AcquireAsync(normalizedOutput, true, cancellationToken)
            .ConfigureAwait(false);

        var workingPresentation = presentationLease.Value;
        var totalSlides = workingPresentation.EnumerateSlides().Count();
        for (var index = totalSlides; index >= 1; index--)
            if (index != templateSlideIdentifier.Index)
                workingPresentation.RemoveSlide(index);

        var outputExtensionType = PresentationExtensions.FromFileExtension(Path.GetExtension(outputPath));
        workingPresentation.Save(outputExtensionType);

        context.SetVariable(WorksheetContextRules.PresentationLease, presentationLease);
        context.SetVariable(WorksheetContextRules.WorkingTemplateSlide, new PresentationIdentifier(outputPath).GetSlide(1));
    }
}
