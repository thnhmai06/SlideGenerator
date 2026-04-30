using SlideGenerator.Application.Modules.Resources.Services;
using SlideGenerator.Application.Modules.Systems.Abstractions;
using SlideGenerator.Application.Modules.Workflows.DSL;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Services.Generating.Workflows.Models;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models.Identifiers;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using SlideGenerator.Domain.Slides.Rules;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     Computes the output file path, copies the template presentation to that path, strips all slides
///     except the designated template slide, and saves the copy.
/// </summary>
/// <remarks>
///     <b>Variables read:</b> <see cref="VariablesDeclaration.WorksheetItem" />.<br />
///     <b>Variables written:</b> <see cref="VariablesDeclaration.OutputPath" />,
///     <see cref="VariablesDeclaration.WorkingTemplateSlide" />.<br />
///     <b>Data read:</b> <see cref="WorkflowTask.Request" /> (<c>Graph</c>, <c>OutputExtension</c>,
///     <c>SaveFolder</c>).<br />
///     <b>Services:</b> <see cref="FileRegistry{IReadOnlyWorkbook}" />, <see cref="FileRegistry{IPresentation}" />,
///     <see cref="IFileSystem" />.<br />
///     <b>CancellationToken:</b> propagated to lease acquires.
/// </remarks>
public sealed class CreateWorkingPresentation(
    FileRegistry<IReadOnlyWorkbook> workbookRegistry,
    FileRegistry<IPresentation> presentationRegistry,
    IFileSystem fileSystem,
    Variable<WorksheetIdentifier> worksheetVar) : ILeafActivity<WorkflowTask>
{
    /// <inheritdoc />
    public async Task ExecuteAsync(IActivityContext<WorkflowTask> context)
    {
        var data = context.Data;
        var worksheet = context.GetVariable(worksheetVar);
        var templateSlideIdentifier = data.Request.Graph[worksheet];

        var extension = data.Request.OutputExtension.ToFileExtension();
        var workbookName = Path.GetFileNameWithoutExtension(worksheet.Workbook.FilePath);
        var outputPath = Path.Combine(data.Request.SaveFolder, $"{workbookName}_{worksheet.Name}{extension}");

        var workbookPath = Path.GetFullPath(worksheet.Workbook.FilePath);
        if (!File.Exists(workbookPath))
            throw new FileNotFoundException("Workbook file not found.", workbookPath);

        await using (var lease = await workbookRegistry.AcquireAsync(workbookPath, false, context.CancellationToken)
                         .ConfigureAwait(false))
        {
            var workbook = lease.Value;
            if (!workbook.TryGetWorksheet(worksheet.Name, out _))
                throw new InvalidOperationException(
                    $"Worksheet '{worksheet.Name}' does not exist in workbook '{workbook.Name}'.");
        }

        var templatePath = Path.GetFullPath(templateSlideIdentifier.Presentation.FilePath);
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("Template presentation file not found.", templatePath);

        fileSystem.CopyFile(templatePath, outputPath);

        var normalizedOutput = Path.GetFullPath(outputPath);
        await using (var lease = await presentationRegistry
                         .AcquireAsync(normalizedOutput, true, context.CancellationToken)
                         .ConfigureAwait(false))
        {
            var workingPresentation = lease.Value;

            var totalSlides = workingPresentation.EnumerateSlides().Count();
            for (var index = totalSlides; index >= 1; index--)
                if (index != templateSlideIdentifier.Index)
                    workingPresentation.RemoveSlide(index);

            var outputExtensionType = PresentationExtensions.FromFileExtension(Path.GetExtension(outputPath));
            workingPresentation.Save(outputExtensionType);
        }

        context.SetVariable(VariablesDeclaration.OutputPath, outputPath);
        context.SetVariable(VariablesDeclaration.WorkingTemplateSlide,
            new PresentationIdentifier(outputPath).GetSlide(WorkflowConstants.WorkingTemplateSlideIndex));
    }
}