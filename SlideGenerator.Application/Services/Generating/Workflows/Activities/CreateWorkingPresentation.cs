using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Application.Modules.Systems.Abstractions;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Modules.Workflows.Rules;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using SlideGenerator.Domain.Slides.Rules;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     Computes the output file path, copies the template presentation to that path, strips all slides
///     except the designated template slide, saves the copy, and holds an open registry lease for
///     further editing steps.
/// </summary>
/// <remarks>
///     <b>Variables read:</b> <see cref="VariablesDeclaration.WorksheetItem" />.<br />
///     <b>Data read:</b> <see cref="WorkflowTask.Request" /> (<c>Graph</c>, <c>OutputExtension</c>,
///     <c>SaveFolder</c>).<br />
///     <b>Data written:</b> <see cref="SheetTask.OutputPath" />, <see cref="SheetTask.WorkingTemplateSlide" />,
///     <see cref="SheetTask.PresentationLease" />
///     — the lease is intentionally kept open and must be disposed by the workflow after all row edits complete.<br />
///     <b>Services:</b> <see cref="FileRegistry{IPresentation}" />, <see cref="FileRegistry{IReadOnlyWorkbook}" />,
///     <c>IFileSystem</c>.<br />
///     <b>CancellationToken:</b> propagated to both registry acquires.
/// </remarks>
public sealed class CreateWorkingPresentation(
    FileRegistry<IPresentation> slideRegistry,
    FileRegistry<IReadOnlyWorkbook> workbookRegistry,
    IFileSystem fileSystem,
    Variable<WorksheetIdentifier> worksheetVar) : ILeafActivity<WorkflowTask>
{
    /// <inheritdoc />
    public async Task ExecuteAsync(IActivityContext<WorkflowTask> context)
    {
        var data = context.Data;
        var worksheet = context.GetVariable(worksheetVar);
        var sheetTask = data.SheetTasks[worksheet];
        var templateSlideIdentifier = data.Request.Graph[worksheet];

        var extension = data.Request.OutputExtension.ToFileExtension();
        var workbookName = Path.GetFileNameWithoutExtension(worksheet.Workbook.FilePath);
        var outputPath = Path.Combine(data.Request.SaveFolder, $"{workbookName}_{worksheet.Name}{extension}");
        sheetTask.OutputPath = outputPath;

        var workbookPath = Path.GetFullPath(worksheet.Workbook.FilePath);
        if (!File.Exists(workbookPath))
            throw new FileNotFoundException("Workbook file not found.", workbookPath);

        using var workbookLease = await workbookRegistry
            .AcquireAsync(workbookPath, false, context.CancellationToken)
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
            .AcquireAsync(normalizedOutput, true, context.CancellationToken)
            .ConfigureAwait(false);

        var workingPresentation = presentationLease.Value;
        var totalSlides = workingPresentation.EnumerateSlides().Count();
        for (var index = totalSlides; index >= 1; index--)
            if (index != templateSlideIdentifier.Index)
                workingPresentation.RemoveSlide(index);

        var outputExtensionType = PresentationExtensions.FromFileExtension(Path.GetExtension(outputPath));
        workingPresentation.Save(outputExtensionType);

        sheetTask.PresentationLease = presentationLease;
        sheetTask.WorkingTemplateSlide =
            new PresentationIdentifier(outputPath).GetSlide(WorkflowConstants.WorkingTemplateSlideIndex);
    }
}