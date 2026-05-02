using SlideGenerator.Slides.Rules;
using SlideGenerator.Workflows.Generating.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;

namespace SlideGenerator.Workflows.Generating.Activities;

public sealed class CreateWorkingPresentation(
    SfWorkbookFactory workbookFactory,
    SfPresentationRegistry presentationRegistry) : StepBodyAsync
{
    public WorksheetMapping Worksheet { get; set; } = null!;
    public GeneratingRequest Request { get; set; } = null!;
    public string OutputPath { get; set; } = null!;
    public Exception? Exception { get; set; }

    public override async Task<ExecutionResult> RunAsync(IStepExecutionContext context)
    {
        try
        {
            var templateSlideMapping = Request.Graph[Worksheet];

            var extension = Request.OutputExtension.ToFileExtension();
            var workbookName = Path.GetFileNameWithoutExtension(Worksheet.Workbook.FilePath);
            var outputPath = Path.Combine(Request.SaveFolder, $"{workbookName}_{Worksheet.WorksheetName}{extension}");

            var workbookPath = Path.GetFullPath(Worksheet.Workbook.FilePath);
            if (!File.Exists(workbookPath))
                throw new FileNotFoundException("Workbook file not found.", workbookPath);

            await using (var lease = await workbookFactory.AcquireAsync(workbookPath, false, context.CancellationToken).ConfigureAwait(false))
            {
                var workbook = lease.Value.Workbook;
                if (workbook.Worksheets[Worksheet.WorksheetName] == null)
                    throw new InvalidOperationException($"Worksheet '{Worksheet.WorksheetName}' does not exist.");
            }

            var templatePath = Path.GetFullPath(templateSlideMapping.PresentationFilePath);
            if (!File.Exists(templatePath))
                throw new FileNotFoundException("Template presentation file not found.", templatePath);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.Copy(templatePath, outputPath, true);

            var normalizedOutput = Path.GetFullPath(outputPath);
            await using (var lease = await presentationRegistry.AcquireAsync(normalizedOutput, true, context.CancellationToken).ConfigureAwait(false))
            {
                var wrapper = lease.Value;
                var presentation = wrapper.Presentation;

                // Syncfusion ISlideCollection is 0-indexed
                for (var i = presentation.Slides.Count; i >= 1; i--)
                    if (i != templateSlideMapping.SlideIndex)
                        presentation.Slides.RemoveAt(i - 1);

                wrapper.Save();
            }

            OutputPath = outputPath;
        }
        catch (Exception ex)
        {
            Exception = ex;
        }

        return ExecutionResult.Next();
    }
}