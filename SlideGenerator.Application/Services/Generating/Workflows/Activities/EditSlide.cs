using SlideGenerator.Application.Modules.Registry.Interfaces;
using SlideGenerator.Application.Modules.Settings.Interfaces;
using SlideGenerator.Application.Modules.Slides.Abstractions;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models.Identifiers;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using WorkflowCore.Interface;
using WorkflowCore.Models;
using ImageSpecializedInstruction = SlideGenerator.Application.Services.Generating.Models.Images.SpecializedInstruction;
using TextGeneralInstruction = SlideGenerator.Application.Services.Generating.Models.Texts.GeneralInstruction;

namespace SlideGenerator.Application.Services.Generating.Workflows.Activities;

/// <summary>
///     A workflow activity that populates a cloned slide with text and image data from a specific row.
/// </summary>
/// <remarks>
///     The editing process includes:
///     <list type="bullet">
///         <item>
///             <description>Locating the specific cloned slide within the working presentation.</description>
///         </item>
///         <item>
///             <description>Resolving row-specific text values and replacing placeholders (e.g., {{Name}}) using the <see cref="ITextComposer"/>.</description>
///         </item>
///         <item>
///             <description>Matching processed images to target shapes and injecting them into the slide using the <see cref="IImageComposer"/>.</description>
///         </item>
///         <item>
///             <description>Validating existence of worksheets and shapes before attempting modification.</description>
///         </item>
///     </list>
/// </remarks>
/// <param name="workbookRegistry">Registry to safely read row data from the workbook.</param>
/// <param name="presentationRegistry">Registry to manage concurrent write access to the presentation file.</param>
/// <param name="textComposer">Service to perform text replacement logic.</param>
/// <param name="imageComposer">Service to perform image injection logic.</param>
/// <param name="settingProvider">Provider for global application settings.</param>
public sealed class EditSlide(
    FileRegistry<IReadOnlyWorkbook> workbookRegistry,
    FileRegistry<IPresentation> presentationRegistry,
    ITextComposer textComposer,
    IImageComposer imageComposer,
    ISettingProvider settingProvider) : PresentationStepBase(presentationRegistry)
{
    /// <summary>
    ///     Gets or sets the row identifier (worksheet and index) providing the source data.
    /// </summary>
    public RowIdentifier Row { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the original template slide identifier (used for metadata reference).
    /// </summary>
    public SlideIdentifier WorkingTemplateSlide { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the absolute path to the working presentation file.
    /// </summary>
    public string OutputPath { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the list of general text instructions to be applied to this row.
    /// </summary>
    public List<TextGeneralInstruction> TextInstructions { get; set; } = [];

    /// <summary>
    ///     Gets or sets the list of fully resolved image instructions (with local paths) to be applied to this row.
    /// </summary>
    public List<ImageSpecializedInstruction> ResolvedImageInstructions { get; set; } = [];

    protected override async Task<ExecutionResult> ExecuteStepAsync(IStepExecutionContext context)
    {
        var slideIndex = WorkflowConstants.WorkingTemplateSlideIndex + Row.Index;
        var presentation = await AcquirePresentationAsync(OutputPath, context.CancellationToken).ConfigureAwait(false);

        var slide = presentation.EnumerateSlides().ElementAtOrDefault(slideIndex - 1)
                    ?? throw new InvalidOperationException(
                        $"Cannot replace contents: slide {slideIndex} does not exist.");

        var textMap = await BuildTextMapAsync(workbookRegistry, context.CancellationToken).ConfigureAwait(false);
        if (textMap is { Count: > 0 })
            foreach (var shape in slide.DescendShapes())
                textComposer.Replace(shape, textMap);

        if (ResolvedImageInstructions is { Count: > 0 })
        {
            var downloadRoot = Path.GetFullPath(settingProvider.Current.Download.DownloadFolder);
            foreach (var shape in slide.DescendShapes())
            {
                var instruction = ResolvedImageInstructions.FirstOrDefault(x => x.Target.Id == shape.Id);
                if (instruction == null) continue;

                var editedPath = instruction.GetEditPath(downloadRoot, Row.Worksheet, Row.Index);
                if (!File.Exists(editedPath)) continue;

                await using var imageStream =
                    new FileStream(editedPath, FileMode.Open, FileAccess.Read, FileShare.Read);

                if (imageStream.CanSeek)
                    imageStream.Position = 0;

                imageComposer.Replace(shape, imageStream);
            }
        }

        return ExecutionResult.Next();
    }

    private async ValueTask<IReadOnlyDictionary<string, string>> BuildTextMapAsync(
        FileRegistry<IReadOnlyWorkbook> registry,
        CancellationToken ct)
    {
        await using var lease = await registry
            .AcquireAsync(Row.Worksheet.Workbook.FilePath, false, ct)
            .ConfigureAwait(false);
        var workbook = lease.Value;

        if (!workbook.TryGetWorksheet(Row.Worksheet.Name, out var ws))
            throw new InvalidOperationException(
                $"Worksheet '{Row.Worksheet.Name}' does not exist in workbook.");

        var rowContent = ws.GetRowContent(Row.Index);

        return TextInstructions
            .Select(general => general.Flatten(general, rowContent)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Value)) ?? general.Empty)
            .ToDictionary(
                x => x.Placeholder,
                x => x.Value,
                StringComparer.Ordinal);
    }
}
