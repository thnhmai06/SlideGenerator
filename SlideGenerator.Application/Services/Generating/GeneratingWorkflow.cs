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

/// <summary>
///     Encapsulates the high-level workflow for generating presentations from worksheet data.
///     Infrastructure executes the activity tree built by this class.
/// </summary>
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
    private const int WorkingTemplateSlideIndex = 1;

    /// <summary>Builds the complete generation activity tree for the given request.</summary>
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
                ], name: "GenerateByWorksheet"), name: "GenerateByWorksheets")
        ], name: "Generation");
    }

    private Sequence BuildPreparingSequence(GeneratingRequest request)
    {
        var downloadState = CreateVariable<KeyValuePair<ImageSpecializedInstruction, string>>(WorksheetContextRules.DownloadItem);
        var editState = CreateVariable<KeyValuePair<ImageSpecializedInstruction, string>>(WorksheetContextRules.EditItem);

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
                    new DownloadImage(downloadRegistry, settingProvider), name: "DownloadImagesInParallel")),
            new ResolveImagePathsFromDisk(settingProvider, false),
            activities.ParallelForEach(LazyItems<KeyValuePair<ImageSpecializedInstruction, string>>(),
                editState,
                activities.SlotGated(SlotType.EditImage,
                    new EditImage(slideRegistry, roiCalculator, imageDecoder, settingProvider), name: "EditImagesInParallel")),
            new ResolveImagePathsFromDisk(settingProvider, true)
        ], name: "Preparing");
    }

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
                    }, name: "LoadRowCount"),
                    activities.ForEach(LazyItems<int>(),
                        rowState,
                        activities.Sequence(
                        [
                            new CloneTemplateSlide(slideRegistry, WorkingTemplateSlideIndex),
                            new ReplaceSlideContents(
                                slideRegistry, textReplacer, imageReplacers, workbookRegistry, WorkingTemplateSlideIndex)
                        ], name: "CloneThenReplaceByRecord"), name: "GenerateSlidesByRecord")
                ]))
            ,
            new RemoveWorkingTemplateSlide(slideRegistry),
            activities.Inline((context, _) =>
            {
                context.GetVariable<IDisposable>(WorksheetContextRules.PresentationLease)?.Dispose();
                return ValueTask.CompletedTask;
            }, name: "CleanupResources")
        ], name: "Editing");
    }

    private static Variable<T> CreateVariable<T>(string? name = null) => new Variable<T> { Name = name };

    private static IEnumerable<T> LazyItems<T>()
    {
        // This is a placeholder. The actual implementation will be in the infrastructure.
        // To build the activity tree, we just need an empty enumerable.
        return [];
    }
}
