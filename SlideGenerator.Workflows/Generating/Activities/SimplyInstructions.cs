using SlideGenerator.Workflows.Generating.Models;
using SlideGenerator.Workflows.Scanning.Models;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using TextGeneralInstruction = SlideGenerator.Workflows.Generating.Models.Texts.GeneralInstruction;

namespace SlideGenerator.Workflows.Generating.Activities;

public sealed class SimplyInstructions : StepBody
{
    public WorksheetMapping Worksheet { get; set; } = null!;
    public GeneratingRequest Request { get; set; } = null!;
    public IDictionary<string, WorkbookSummary> WorkbookSummaries { get; set; } = null!;
    public IDictionary<string, PresentationSummary> PresentationSummaries { get; set; } = null!;

    public List<int> RowIndices { get; set; } = [];
    public List<TextGeneralInstruction> TextInstructions { get; set; } = [];
    public List<GeneralInstruction> ImageInstructions { get; set; } = [];
    public Exception? Exception { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        try
        {
            var templateSlideMapping = Request.Graph[Worksheet];

            var presentationPath = Path.GetFullPath(templateSlideMapping.PresentationFilePath);
            if (!PresentationSummaries.TryGetValue(presentationPath, out var presentationSummary))
                throw new InvalidOperationException($"Presentation summary for '{presentationPath}' was not found.");

            var slideSummary = presentationSummary.Slides.FirstOrDefault(s => s.Index == templateSlideMapping.SlideIndex)
                               ?? throw new InvalidOperationException(
                                   $"Slide {templateSlideMapping.SlideIndex} not found in presentation summary.");

            var placeholders = slideSummary.Placeholders.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var shapeNames = slideSummary.ImageShapes.Select(s => s.Name).ToHashSet();

            var workbookPath = Path.GetFullPath(Worksheet.Workbook.FilePath);
            if (!WorkbookSummaries.TryGetValue(workbookPath, out var workbookSummary))
                throw new InvalidOperationException($"Workbook summary for '{workbookPath}' was not found.");

            var worksheetSummary = workbookSummary.Worksheets
                                       .FirstOrDefault(w =>
                                           string.Equals(w.Name, Worksheet.WorksheetName, StringComparison.OrdinalIgnoreCase))
                                   ?? throw new InvalidOperationException(
                                       $"Worksheet '{Worksheet.WorksheetName}' not found.");

            var headers = worksheetSummary.Preview.Headers;

            TextInstructions = Request.TextInstructions
                .Where(x => placeholders.Contains(x.Placeholder)
                            && headers.Contains(x.Placeholder, StringComparer.OrdinalIgnoreCase))
                .ToList();

            ImageInstructions = Request.ImageInstructions
                .Where(x => shapeNames.Contains(x.TargetShapeName)
                            && x.SourceColumns.Any(s =>
                                headers.Contains(s, StringComparer.OrdinalIgnoreCase)))
                .ToList();

            RowIndices = Enumerable.Range(1, worksheetSummary.Count).ToList();
        }
        catch (Exception ex)
        {
            Exception = ex;
        }

        return ExecutionResult.Next();
    }
}