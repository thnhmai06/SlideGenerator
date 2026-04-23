using SlideGenerator.Application.Cloud.Abstractions;
using SlideGenerator.Application.Download.Services;
using SlideGenerator.Application.Images.Abstractions;
using SlideGenerator.Application.Resources.Services;
using SlideGenerator.Application.Services.Generating.Activities;
using SlideGenerator.Application.Services.Generating.Models;
using SlideGenerator.Application.Services.Generating.Rules;
using SlideGenerator.Application.Settings.Interfaces;
using SlideGenerator.Application.Slides.Abstractions;
using SlideGenerator.Application.Systems.Abstractions;
using SlideGenerator.Application.Workflows.Abstractions;
using SlideGenerator.Application.Workflows.Entities.Activities;
using SlideGenerator.Application.Workflows.Entities.Contexts;
using SlideGenerator.Application.Workflows.Entities.Workflows;
using SlideGenerator.Domain.Images.Entities;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using ImageSpecializedInstruction = SlideGenerator.Application.Services.Generating.Models.Images.SpecializedInstruction;

namespace SlideGenerator.Application.Services.Generating;

/// <summary>Generates presentations from worksheet data.</summary>
/// <remarks>Infrastructure executes the activity tree built by this class.</remarks>
/// <param name="activities">The activity factory.</param>
/// <param name="slideRegistry">The presentation file registry.</param>
/// <param name="workbookRegistry">The workbook file registry.</param>
/// <param name="textReplacer">The text replacement service.</param>
/// <param name="imageReplacers">The collection of image replacement services.</param>
/// <param name="cloudResolver">The cloud URL resolver.</param>
/// <param name="downloadRegistry">The image download registry.</param>
/// <param name="roiCalculator">The region of interest calculator.</param>
/// <param name="imageDecoder">The image decoding service.</param>
/// <param name="settingProvider">The settings provider.</param>
/// <param name="fileSystem">The file system abstraction.</param>
public sealed class GeneratingWorkflow(
    IActivityFactory activities,
    FileRegistry<IPresentation> slideRegistry,
    FileRegistry<IReadOnlyWorkbook> workbookRegistry,
    ITextReplacer textReplacer,
    IEnumerable<IImageReplacer> imageReplacers,
    ICloudResolver cloudResolver,
    DownloadRegistry downloadRegistry,
    IRoiCalculator roiCalculator,
    IImageDecoder imageDecoder,
    ISettingProvider settingProvider,
    IFileSystem fileSystem) : Workflow<GeneratingRequest>
{
    /// <summary>Index of the working template slide.</summary>
    private const int WorkingTemplateSlideIndex = 1;

    /// <summary>Builds the activity tree.</summary>
    /// <param name="request">The generation request.</param>
    /// <returns>The workflow <see cref="Activity" />.</returns>
    public override Activity Build(GeneratingRequest request)
    {
        var worksheetState = CreateVariable<WorksheetIdentifier?>(WorksheetContextRules.Worksheet);

        return activities.Sequence(
        [
            activities.ParallelForEach(request.Graph.Keys,
                worksheetState,
                activities.Sequence(
                [
                    BuildPreparingSequence(request),
                    BuildEditingSequence()
                ], "GenerateByWorksheet"), "GenerateByWorksheets")
        ], "Generation");
    }

    /// <summary>Builds the preparation sequence.</summary>
    /// <param name="request">The generation request.</param>
    /// <returns>A <see cref="Sequence" /> for preparation tasks.</returns>
    private Sequence BuildPreparingSequence(GeneratingRequest request)
    {
        var downloadState =
            CreateVariable<KeyValuePair<ImageSpecializedInstruction, string>>(WorksheetContextRules.DownloadItem);
        var editState =
            CreateVariable<KeyValuePair<ImageSpecializedInstruction, string>>(WorksheetContextRules.EditItem);

        return activities.Sequence(
        [
            new BuildOutputPath(request.SaveFolder, request.OutputExtension),
            new CreateWorkingPresentation(slideRegistry, workbookRegistry, fileSystem, request.Graph),
            new ScanTemplateContent(slideRegistry, textReplacer),
            new SpecializeInstructions(workbookRegistry, request.TextInstructions, request.ImageInstructions),
            new ResolveImageUrls(cloudResolver, workbookRegistry),
            activities.ParallelForEach(LazyItems<KeyValuePair<ImageSpecializedInstruction, string>>(),
                downloadState,
                activities.SlotGated(SlotType.Download,
                    new DownloadImage(downloadRegistry, settingProvider), "DownloadImagesInParallel")),
            new ResolveImagePathsFromDisk(settingProvider, false),
            activities.ParallelForEach(LazyItems<KeyValuePair<ImageSpecializedInstruction, string>>(),
                editState,
                activities.SlotGated(SlotType.EditImage,
                    new EditImage(slideRegistry, roiCalculator, imageDecoder, settingProvider),
                    "EditImagesInParallel")),
            new ResolveImagePathsFromDisk(settingProvider, true)
        ], "Preparing");
    }

    /// <summary>Builds the editing sequence.</summary>
    /// <returns>A <see cref="Sequence" /> for editing tasks.</returns>
    private Sequence BuildEditingSequence()
    {
        var rowState = CreateVariable<int>(WorksheetContextRules.Row);

        return activities.Sequence(
        [
            activities.SlotGated(SlotType.EditSlide,
                activities.Sequence(
                [
                    activities.Inline(async (context, ct) =>
                    {
                        var worksheet = context.GetVariable<WorksheetIdentifier>(WorksheetContextRules.Worksheet)!;
                        using var lease = await workbookRegistry
                            .AcquireAsync(worksheet.Workbook.FilePath, true, ct)
                            .ConfigureAwait(false);
                        var workbook = lease.Value;
                        if (!workbook.TryGetWorksheet(worksheet.Name, out var ws))
                            throw new InvalidOperationException(
                                $"Worksheet '{worksheet.Name}' does not exist in workbook.");
                        context.SetVariable(WorksheetContextRules.RowCount, ws.RowsCount);
                    }, "LoadRowCount"),
                    activities.ForEach(LazyItems<int>(),
                        rowState,
                        activities.Sequence(
                        [
                            new CloneTemplateSlide(slideRegistry, WorkingTemplateSlideIndex),
                            new ReplaceSlideContents(
                                slideRegistry, textReplacer, imageReplacers, workbookRegistry,
                                WorkingTemplateSlideIndex)
                        ], "CloneThenReplaceByRecord"), "GenerateSlidesByRecord")
                ])),
            new RemoveWorkingTemplateSlide(slideRegistry),
            activities.Inline((context, _) =>
            {
                context.GetVariable<IDisposable>(WorksheetContextRules.PresentationLease)?.Dispose();
                return ValueTask.CompletedTask;
            }, "CleanupResources")
        ], "Editing");
    }

    /// <summary>Creates a workflow variable.</summary>
    /// <typeparam name="T">The variable type.</typeparam>
    /// <param name="name">The variable name.</param>
    /// <returns>A new <see cref="Variable{T}" />.</returns>
    private static Variable<T> CreateVariable<T>(string? name = null)
    {
        return new Variable<T> { Name = name };
    }

    /// <summary>Provides an empty lazy enumerable for iteration.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>An empty <see cref="IEnumerable{T}" />.</returns>
    private static IEnumerable<T> LazyItems<T>()
    {
        // This is a placeholder. The actual implementation will be in the infrastructure.
        // To build the activity tree, we just need an empty enumerable.
        return [];
    }
}