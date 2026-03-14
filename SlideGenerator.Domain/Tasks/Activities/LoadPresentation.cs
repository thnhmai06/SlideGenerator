using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using SlideGenerator.Domain.Tasks.Models;

namespace SlideGenerator.Domain.Tasks.Activities;

/// <summary>
///     Workflow step that prepares an output presentation from a template and opens it for downstream processing.
/// </summary>
/// <remarks>
///     <para>
///         The workflow runs two inline activities:
///     </para>
///     <list type="number">
///         <item>
///             <description>
///                 <c>PreparePresentation</c> loads the template into memory, keeps only <see cref="TemplateIndex" />,
///                 converts to <see cref="PresentationExtension" />, writes the result to <see cref="SaveFolder" />,
///                 and sets <see cref="PresentationPath" />.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <c>OpenPresentation</c> opens the generated file and stores the
///                 <see cref="PresentationDocument" /> in <c>WorkflowExecutionContext.TransientProperties</c>
///                 under key <c>Presentation</c>.
///             </description>
///         </item>
///     </list>
///
///     <para>
///     The following keys are written to:
///        <code>context.WorkflowExecutionContext.TransientProperties</code>
///        <list>
///           <item>+ <c>Presentation</c>: the open <see cref="PresentationDocument" /> instance for the generated presentation file.</item>
///        </list>
///     </para>
/// </remarks>
public sealed class LoadPresentation : WorkflowBase
{
    /// <summary>
    ///     Input: File path and index of the template slide to use.
    /// </summary>
    public Input<SlideInfo> TemplateInfo { get; set; } = null!;

    /// <summary>
    ///     Input: Output folder for the generated presentation file.
    /// </summary>
    public Input<string> SaveFolder { get; set; } = null!;

    /// <summary>
    ///     Input: Output file name without extension.
    /// </summary>
    public Input<string> PresentationName { get; set; } = null!;

    /// <summary>
    ///     Input: Output presentation extension and OpenXML document type.
    /// </summary>
    public Input<OutputExtension> PresentationExtension { get; set; } = null!;

    /// <summary>
    ///     Output: Full path of the generated presentation file.
    /// </summary>
    public Output<string> PresentationPath { get; set; } = null!;

    /// <summary>
    ///     Configures the sequence that prepares and then opens the generated presentation.
    /// </summary>
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                PreparePresentation,
                OpenPresentation
            },
            Name = "LoadPresentation"
        };
    }

    /// <summary>
    ///     Creates the output presentation file by trimming template slides and applying the requested format.
    /// </summary>
    private Inline PreparePresentation => new(context =>
    {
        // Get input values
        var templateInfo = context.Get(TemplateInfo);
        var saveFolder = context.Get(SaveFolder);
        var fileName = context.Get(PresentationName);
        var outputExtension = context.Get(PresentationExtension).ToFileExtension();
        var outputType = context.Get(PresentationExtension).ToPresentationDocumentType();

        // Validate inputs
        if (string.IsNullOrEmpty(templateInfo?.FilePath) ||
            string.IsNullOrEmpty(saveFolder) ||
            string.IsNullOrEmpty(fileName) ||
            templateInfo.Index <= 0)
            return;

        // Prepare output directory and path
        Directory.CreateDirectory(saveFolder);
        var outputPath = Path.Combine(saveFolder, $"{fileName}{outputExtension}");

        // Step 1: Load template into memory stream
        var bytes = File.ReadAllBytes(templateInfo.FilePath);
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
                if (index != templateInfo.Index)
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
        context.Set(PresentationPath, outputPath);
    })
    {
        Name = "PreparePresentation"
    };

    /// <summary>
    ///     Opens the generated presentation file and stores it in transient properties for later workflow steps.
    /// </summary>
    private Inline OpenPresentation => new(context =>
    {
        var filePath = context.Get(PresentationPath);
        if (string.IsNullOrEmpty(filePath)) return;

        var doc = PresentationDocument.Open(filePath, true);
        context.WorkflowExecutionContext.TransientProperties["Presentation"] = doc;
    })
    {
        Name = "OpenPresentation"
    };
}