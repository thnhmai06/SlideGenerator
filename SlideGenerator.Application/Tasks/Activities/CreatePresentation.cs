using Elsa.Workflows;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Common;
using SlideGenerator.Domain.Sheet.Entities;
using SlideGenerator.Domain.Sheet.Models;
using SlideGenerator.Domain.Slide.Entities;
using SlideGenerator.Domain.Slide.Rules;
using SlideGenerator.Domain.Slide.Models;

namespace SlideGenerator.Application.Tasks.Activities;

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
///                 <c>PreparePresentation</c> loads the template into memory, keeps only <see cref="TemplateInfo" />,
///                 converts to <see cref="PresentationExtension" />, writes the result to <see cref="OutputPath" />,
///                 and opens the cloned output via presentation registry.
///             </description>
///         </item>
///     </list>
/// </remarks>
public sealed class CreatePresentation : Activity
{
    /// <summary>
    ///     Input: File path and index of the template slide to use.
    /// </summary>
    public Input<SlideIdentifier> TemplateInfo { get; set; } = null!;

    /// <summary>
    ///     Input: Target worksheet that must exist in the source workbook before creating output presentation.
    /// </summary>
    public Input<WorksheetIdentifier> Worksheet { get; set; } = null!;

    /// <summary>
    ///     Input: Full output path for the generated presentation file.
    /// </summary>
    public Input<string> OutputPath { get; set; } = null!;

    /// <summary>
    ///     Input: Output presentation extension.
    /// </summary>
    public Input<PresentationExtension> PresentationExtension { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the slide registry dependency (injected by DI).
    /// </summary>
    public IRegistry<IPresentation> SlideRegistry { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the workbook registry dependency (injected by DI).
    /// </summary>
    public IRegistry<IReadOnlyWorkbook> WorkbookRegistry { get; set; } = null!;

    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var templateInfo = context.Get(TemplateInfo);
        if (templateInfo is null || string.IsNullOrWhiteSpace(templateInfo.Presentation.FilePath) ||
            templateInfo.Index <= 0)
            return ValueTask.CompletedTask;

        var outputPath = context.Get(OutputPath);
        var outputExtensionType = context.Get(PresentationExtension);
        var worksheet = context.Get(Worksheet);

        if (string.IsNullOrWhiteSpace(outputPath) || worksheet is null)
            return ValueTask.CompletedTask;

        var workbookPath = Path.GetFullPath(worksheet.Workbook.FilePath);
        if (!File.Exists(workbookPath))
            throw new FileNotFoundException("Workbook file not found.", workbookPath);

        var workbook = WorkbookRegistry.GetOrOpen(workbookPath, isEditable: false);
        if (!workbook.TryGetWorksheet(worksheet.Name, out _))
            throw new InvalidOperationException(
                $"Worksheet '{worksheet.Name}' does not exist in workbook '{workbook.Name}'.");

        var templatePath = Path.GetFullPath(templateInfo.Presentation.FilePath);
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("Template presentation file not found.", templatePath);

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        var templatePresentation = SlideRegistry.GetOrOpen(templatePath, isEditable: false);
        try
        {
            // Clone template to output file before mutating slide structure.
            templatePresentation.SaveAs(outputPath, outputExtensionType);
        }
        finally
        {
            SlideRegistry.Close(templatePath);
        }

        var workingPresentation = SlideRegistry.GetOrOpen(outputPath, isEditable: true);

        var totalSlides = workingPresentation.EnumerateSlides().Count();
        for (var index = totalSlides; index >= 1; index--)
        {
            if (index != templateInfo.Index)
                workingPresentation.RemoveSlide(index);
        }

        workingPresentation.Save(outputExtensionType);
        return ValueTask.CompletedTask;
    }
}