using SlideGenerator.Application.Modules.Registry.Interfaces;
using SlideGenerator.Application.Modules.Systems.Abstractions;
using SlideGenerator.Application.Services.Generating.Models;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models.Identifiers;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using SlideGenerator.Domain.Slides.Rules;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     A workflow activity that creates a working copy of the template presentation for a specific worksheet.
/// </summary>
/// <remarks>
///     The creation process includes:
///     <list type="bullet">
///         <item>
///             <description>Validating the existence of the workbook and the specific worksheet.</description>
///         </item>
///         <item>
///             <description>Copying the template presentation file to the designated output folder.</description>
///         </item>
///         <item>
///             <description>Opening the new presentation and removing all slides except for the designated template slide.</description>
///         </item>
///         <item>
///             <description>Saving the cleaned-up presentation as the "working" file for subsequent row generation.</description>
///         </item>
///     </list>
/// </remarks>
/// <param name="workbookRegistry">Registry for workbook access validation.</param>
/// <param name="presentationRegistry">Registry for managed presentation file operations.</param>
/// <param name="fileSystem">Abstraction for file system operations.</param>
public sealed class CreateWorkingPresentation(
    FileRegistry<IReadOnlyWorkbook> workbookRegistry,
    FileRegistry<IPresentation> presentationRegistry,
    IFileSystem fileSystem) : StepBodyAsync
{
    /// <summary>
    ///     Gets or sets the worksheet identifier for which the presentation is being created.
    /// </summary>
    public WorksheetIdentifier Worksheet { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the original generation request containing output settings and mappings.
    /// </summary>
    public GeneratingRequest Request { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the absolute path to the newly created working presentation.
    /// </summary>
    public string OutputPath { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the identifier of the single slide remaining in the working presentation, used as a template for cloning.
    /// </summary>
    public SlideIdentifier WorkingTemplateSlide { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the exception if the creation process failed.
    /// </summary>
    public Exception? Exception { get; set; }

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        try
        {
            var templateSlideIdentifier = Request.Graph[Worksheet];

            var extension = Request.OutputExtension.ToFileExtension();
            var workbookName = Path.GetFileNameWithoutExtension(Worksheet.Workbook.FilePath);
            var outputPath = Path.Combine(Request.SaveFolder, $"{workbookName}_{Worksheet.Name}{extension}");

            var workbookPath = Path.GetFullPath(Worksheet.Workbook.FilePath);
            if (!File.Exists(workbookPath))
                throw new FileNotFoundException("Workbook file not found.", workbookPath);

            await using (var lease = await workbookRegistry.AcquireAsync(workbookPath, false, context.CancellationToken)
                             .ConfigureAwait(false))
            {
                var workbook = lease.Value;
                if (!workbook.TryGetWorksheet(Worksheet.Name, out _))
                    throw new InvalidOperationException(
                        $"Worksheet '{Worksheet.Name}' does not exist in workbook '{workbook.Name}'.");
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

            OutputPath = outputPath;
            WorkingTemplateSlide =
                new PresentationIdentifier(outputPath).GetSlide(WorkflowConstants.WorkingTemplateSlideIndex);
        }
        catch (Exception ex)
        {
            Exception = ex;
        }

        return ExecutionResult.Next();
    }
}
