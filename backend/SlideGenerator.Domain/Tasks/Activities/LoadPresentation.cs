using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using SlideGenerator.Domain.Tasks.Models;

namespace SlideGenerator.Domain.Tasks.Activities;

/// <summary>
///     Workflow step to load a presentation template and keep only a specific slide.
///     Loads template from file into memory, removes all slides except the specified one,
///     changes document type if needed, and saves to output path.
/// </summary>
/// <remarks>
///     <para>
///         This workflow performs the following steps:
///     </para>
///     <list type="number">
///         <item>
///             <description>
///                 <strong>Validate Inputs:</strong> Checks that all required inputs (TemplatePath, SaveFolder,
///                 FileName, TemplateIndex) are provided and valid.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>Load Template:</strong> Reads the template file into a MemoryStream and opens it as
///                 a PresentationDocument for in-memory editing. Supports both .pptx and .potx formats.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>Remove Unwanted Slides:</strong> Iterates through all slides from end to start
///                 (to avoid index shifting) and removes all slides except the one at TemplateIndex (1-based).
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>Change Document Type:</strong> Converts the document to the specified output format
///                 using ChangeDocumentType based on FileExtension input.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <strong>Save to Output:</strong> Copies the modified presentation from memory to the output
///                 file at the specified SaveFolder with the given FileName and FileExtension.
///             </description>
///         </item>
///     </list>
///     <para>
///         The entire process occurs in memory for efficiency - the template file is not modified,
///         and only the final result is written to disk at the output path.
///     </para>
/// </remarks>
public sealed class LoadPresentation : WorkflowBase
{
    /// <summary>
    ///     Input: Path to the template presentation file (.pptx or .potx).
    /// </summary>
    public Input<string> TemplatePath { get; set; } = null!;

    /// <summary>
    ///     Input: Slide index (1-based) to keep in the output. All other slides will be removed.
    /// </summary>
    public Input<int> TemplateIndex { get; set; } = null!;

    /// <summary>
    ///     Input: Folder where the output presentation will be saved.
    /// </summary>
    public Input<string> SaveFolder { get; set; } = null!;

    /// <summary>
    ///     Input: Output file name without extension (e.g., "slide1").
    /// </summary>
    public Input<string> FileName { get; set; } = null!;

    /// <summary>
    ///     Input: Output file extension format (.pptx or .potx).
    /// </summary>
    public Input<OutputExtension> FileExtension { get; set; } = null!;

    /// <summary>
    ///     Output: Full file path to the saved presentation file.
    /// </summary>
    public Output<string> FilePath { get; set; } = null!;

    /// <summary>
    ///     Loads template into memory, removes unwanted slides, changes document type, and saves to output path.
    /// </summary>
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new Inline(context =>
                {
                    // Get input values
                    var templatePath = context.Get(TemplatePath);
                    var saveFolder = context.Get(SaveFolder);
                    var fileName = context.Get(FileName);
                    var templateIndex = context.Get(TemplateIndex);
                    var outputExtension = context.Get(FileExtension).ToFileExtension();
                    var outputType = context.Get(FileExtension).ToPresentationDocumentType();

                    // Validate inputs
                    if (string.IsNullOrEmpty(templatePath) ||
                        string.IsNullOrEmpty(saveFolder) ||
                        string.IsNullOrEmpty(fileName) ||
                        templateIndex <= 0)
                        return;

                    // Prepare output directory and path
                    Directory.CreateDirectory(saveFolder);
                    var outputPath = Path.Combine(saveFolder, $"{fileName}{outputExtension}");

                    // Step 1: Load template into memory stream
                    var bytes = File.ReadAllBytes(templatePath);
                    using var memoryStream = new MemoryStream(bytes);
                    using var doc = PresentationDocument.Open(memoryStream, true);

                    // Step 2: Remove unwanted slides (keep only templateIndex)
                    var slideIdList = doc.PresentationPart?.Presentation?.SlideIdList;
                    if (slideIdList != null)
                    {
                        var slideIds = slideIdList.ChildElements.Cast<SlideId>().ToList();
                        var slideCount = slideIds.Count;
                        // Remove from end to start to avoid index shifting
                        for (var index = slideCount; index >= 1; index--)
                            if (index != templateIndex)
                                slideIds[index - 1].Remove();
                    }

                    // Step 3: Change document type and save changes to memory stream
                    doc.ChangeDocumentType(outputType);
                    doc.Save();

                    // Step 4: Copy modified content to output file
                    memoryStream.Position = 0;
                    using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                    {
                        memoryStream.CopyTo(outputStream);
                    }

                    // Set output value
                    context.Set(FilePath, outputPath);
                })
                {
                    Name = "PreparePresentation"
                },
                new Inline(context =>
                {
                    var filePath = context.Get(FilePath);
                    if (string.IsNullOrEmpty(filePath)) return;

                    var doc = PresentationDocument.Open(filePath, true);
                    context.WorkflowExecutionContext.TransientProperties["Presentation"] = doc;
                })
                {
                    Name = "LoadPresentation"
                }
            }
        };
    }
}