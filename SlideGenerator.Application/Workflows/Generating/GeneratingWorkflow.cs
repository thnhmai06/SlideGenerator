using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using SlideGenerator.Application.Cloud.Services;
using SlideGenerator.Application.Resources;
using SlideGenerator.Application.Slides.Abstractions;
using SlideGenerator.Application.Systems.Abstractions;
using SlideGenerator.Application.Workflows.Generating.Activities;
using SlideGenerator.Application.Workflows.Generating.Models;
using SlideGenerator.Application.Workflows.Generating.Models.Texts;
using SlideGenerator.Domain.Download.Abstractions;
using SlideGenerator.Domain.Settings.Interfaces;
using SlideGenerator.Domain.Sheets.Entities;
using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using SlideGenerator.Domain.Slides.Rules;
using TextSpecializedInstruction = SlideGenerator.Application.Workflows.Generating.Models.Texts.SpecializedInstruction;
using ImageSpecializedInstruction = SlideGenerator.Application.Workflows.Generating.Models.Images.SpecializedInstruction;

namespace SlideGenerator.Application.Workflows.Generating;

/// <summary>
///     Orchestrates worksheet generation with process-wide activity concurrency gates for the Preparing and Editing flows.
/// </summary>
public sealed class GeneratingWorkflow(
    Registry<IReadOnlyWorkbook> workbookRegistry,
    Registry<IPresentation> slideRegistry,
    ITextReplacer textReplacer,
    IEnumerable<IImageReplacer> imageReplacers,
    IFileSystem fileSystem,
    ICloudResolver cloudResolver,
    IDownloadRegistry downloadRegistry,
    ISettingProvider settingProvider) : WorkflowBase
{
    private const string RequestRefKey = GeneratingRequest.Name;
    private const int WorkingTemplateSlideIndex = 1;

    private static GeneratingRequest? GetRequest(ExpressionExecutionContext context)
    {
        return (GeneratingRequest?)context.Get(RequestRefKey);
    }

    /// <inheritdoc />
    protected override void Build(IWorkflowBuilder builder)
    {
        _ = builder.WithInput<GeneratingRequest>(RequestRefKey, GeneratingRequest.Description);

        const string outputPathRefKey = "OutputPath";
        const string workingTemplateSlideRefKey = "WorkingTemplateSlide";
        const string currentRowIndexRefKey = "CurrentRowIndex";
        const string templatePlaceholdersRefKey = "TemplatePlaceholders";
        const string templateImageShapeIdsRefKey = "TemplateImageShapeIds";
        const string specializedTextInstructionsRefKey = "SpecializedTextInstructions";
        const string specializedImageInstructionsRefKey = "SpecializedImageInstructions";
        const string resolvedImageUrlsRefKey = "ResolvedImageUrls";
        const string downloadedImagePathsRefKey = "DownloadedImagePaths";

        builder.Root = new Sequence
        {
            Name = "Generation",
            Activities =
            {
                new ParallelForEach<WorksheetIdentifier>
                {
                    Name = "GenerateByWorksheets",
                    Items = new Input<object>(context => GetRequest(context)!.Graph.Keys.ToList()),
                    Body = new Sequence
                    {
                        Name = "GenerateByWorksheet",
                        Activities =
                        {
                            new Sequence
                            {
                                Name = "Preparing",
                                Activities =
                                {
                                    new BuildOutputPath
                                    {
                                        SaveFolder = new Input<string>(context => GetRequest(context)!.SaveFolder),
                                        Worksheet = new Input<WorksheetIdentifier>(context =>
                                            context.GetVariable<WorksheetIdentifier>("CurrentValue")!),
                                        Extension = new Input<PresentationExtension>(context =>
                                            GetRequest(context)!.OutputExtension),
                                        OutputPath = new Output<string>(Utilities.GetRef(outputPathRefKey))
                                    },
                                    new CreateWorkingPresentation(slideRegistry, workbookRegistry, fileSystem)
                                    {
                                        TemplateSlide = new Input<SlideIdentifier>(context =>
                                        {
                                            var request = GetRequest(context)!;
                                            var worksheet = context.GetVariable<WorksheetIdentifier>("CurrentValue")!;
                                            return request.Graph[worksheet];
                                        }),
                                        Worksheet = new Input<WorksheetIdentifier>(context =>
                                            context.GetVariable<WorksheetIdentifier>("CurrentValue")!),
                                        OutputPath = new Input<string>(Utilities.GetRef(outputPathRefKey)),
                                        WorkingTemplateSlide =
                                            new Output<SlideIdentifier>(Utilities.GetRef(workingTemplateSlideRefKey))
                                    },
                                    new ScanTemplateContent(slideRegistry, textReplacer)
                                    {
                                        TemplateSlide =
                                            new Input<SlideIdentifier>(Utilities.GetRef(workingTemplateSlideRefKey)),
                                        Placeholders =
                                            new Output<IReadOnlySet<string>>(
                                                Utilities.GetRef(templatePlaceholdersRefKey)),
                                        ImageShapeIds =
                                            new Output<IReadOnlySet<uint>>(
                                                Utilities.GetRef(templateImageShapeIdsRefKey))
                                    },
                                    new SpecializeInstructions(workbookRegistry)
                                    {
                                        Worksheet = new Input<WorksheetIdentifier>(context =>
                                            context.GetVariable<WorksheetIdentifier>("CurrentValue")!),
                                        TemplateSlide =
                                            new Input<SlideIdentifier>(Utilities.GetRef(workingTemplateSlideRefKey)),
                                        RawTextInstructions =
                                            new Input<IReadOnlyList<GeneralInstruction>>(context =>
                                                GetRequest(context)!.TextInstructions),
                                        RawImageInstructions =
                                            new Input<IReadOnlyList<Models.Images.GeneralInstruction>>(context => GetRequest(context)!.ImageInstructions),
                                        TemplatePlaceholders =
                                            new Input<IReadOnlySet<string>>(
                                                Utilities.GetRef(templatePlaceholdersRefKey)),
                                        TemplateImageShapeIds =
                                            new Input<IReadOnlySet<uint>>(
                                                Utilities.GetRef(templateImageShapeIdsRefKey)),
                                        TextInstructions =
                                            new Output<IReadOnlyList<SpecializedInstruction>>(
                                                Utilities.GetRef(specializedTextInstructionsRefKey)),
                                        ImageInstructions =
                                            new Output<IReadOnlyList<ImageSpecializedInstruction>>(
                                                Utilities.GetRef(specializedImageInstructionsRefKey))
                                    },
                                    new ResolveImageUrls(cloudResolver, workbookRegistry)
                                    {
                                        ImageInstructions =
                                            new Input<IReadOnlyList<ImageSpecializedInstruction>>(
                                                Utilities.GetRef(specializedImageInstructionsRefKey)),
                                        RowIndex = new Input<int>(1),
                                        WorksheetInfo = new Input<WorksheetIdentifier>(context =>
                                            context.GetVariable<WorksheetIdentifier>("CurrentValue")!),
                                        ResolvedImageUrls =
                                            new Output<IReadOnlyDictionary<ImageSpecializedInstruction, string>>(
                                                Utilities.GetRef(resolvedImageUrlsRefKey))
                                    },
                                    new AcquirePreparingSlot(settingProvider),
                                    new DownloadImages(downloadRegistry, settingProvider)
                                    {
                                        ImageUrls = new Input<IReadOnlyDictionary<ImageSpecializedInstruction, string>>(
                                            Utilities.GetRef(resolvedImageUrlsRefKey)),
                                        Worksheet = new Input<WorksheetIdentifier>(context =>
                                            context.GetVariable<WorksheetIdentifier>("CurrentValue")!),
                                        RowIndex = new Input<int>(1),
                                        ImagePaths =
                                            new Output<IReadOnlyDictionary<ImageSpecializedInstruction, string>>(
                                                Utilities.GetRef(downloadedImagePathsRefKey))
                                    },
                                    new EditImages
                                    {
                                        DownloadedImagePaths =
                                            new Input<IReadOnlyDictionary<ImageSpecializedInstruction, string>>(
                                                Utilities.GetRef(downloadedImagePathsRefKey))
                                    },
                                    new ReleasePreparingSlot()
                                }
                            },
                            new Sequence
                            {
                                Name = "Editing",
                                Activities =
                                {
                                    new AcquireEditingSlot(settingProvider),
                                    new ForEach<int>
                                    {
                                        Name = "GenerateSlidesByRecord",
                                        Items = new Input<ICollection<int>>(context =>
                                        {
                                            var worksheet = context.GetVariable<WorksheetIdentifier>("CurrentValue")!;
                                            using var workbookLease =
                                                workbookRegistry.Acquire(worksheet.Workbook.FilePath, true);
                                            var workbook = workbookLease.Value;

                                            if (!workbook.TryGetWorksheet(worksheet.Name, out var readOnlyWorksheet))
                                                throw new InvalidOperationException(
                                                    $"Worksheet '{worksheet.Name}' does not exist in workbook.");

                                            var rowCount = readOnlyWorksheet.RowsCount;
                                            return Enumerable.Range(1, rowCount).ToList();
                                        }),
                                        CurrentValue = new Output<int>(Utilities.GetRef(currentRowIndexRefKey)),
                                        Body = new Sequence
                                        {
                                            Name = "CloneThenReplaceByRecord",
                                            Activities =
                                            {
                                                new CloneTemplateSlide(slideRegistry)
                                                {
                                                    TemplateSlide =
                                                        new Input<SlideIdentifier>(
                                                            Utilities.GetRef(workingTemplateSlideRefKey)),
                                                    InsertAtIndex = new Input<int>(context =>
                                                    {
                                                        var rowIndex =
                                                            context.Get<int>(Utilities.GetRef(currentRowIndexRefKey));
                                                        return WorkingTemplateSlideIndex + rowIndex;
                                                    })
                                                },
                                                new ReplaceSlideContents(slideRegistry, textReplacer, imageReplacers)
                                                {
                                                    SlideIdentifier = new Input<SlideIdentifier>(context =>
                                                    {
                                                        var rowIndex =
                                                            context.Get<int>(Utilities.GetRef(currentRowIndexRefKey));
                                                        var workingTemplateSlide = context.Get<SlideIdentifier>(
                                                            Utilities.GetRef(workingTemplateSlideRefKey))!;
                                                        return workingTemplateSlide.Presentation.GetSlide(
                                                            WorkingTemplateSlideIndex + rowIndex);
                                                    }),
                                                    TextInstructions =
                                                        new Input<IReadOnlyDictionary<string, string>>(context =>
                                                        {
                                                            var worksheet =
                                                                context.GetVariable<WorksheetIdentifier>("CurrentValue")
                                                                !;
                                                            var rowIndex =
                                                                context.Get<int>(
                                                                    Utilities.GetRef(currentRowIndexRefKey));
                                                            using var workbookLease = workbookRegistry.Acquire(
                                                                worksheet.Workbook.FilePath, true);
                                                            var workbook = workbookLease.Value;

                                                            if (!workbook.TryGetWorksheet(worksheet.Name,
                                                                    out var readOnlyWorksheet))
                                                                throw new InvalidOperationException(
                                                                    $"Worksheet '{worksheet.Name}' does not exist in workbook.");

                                                            var rowContent = readOnlyWorksheet.GetRowContent(rowIndex);
                                                            var instructions =
                                                                (IReadOnlyList<TextSpecializedInstruction>)context.Get(
                                                                    specializedTextInstructionsRefKey)!;

                                                            return instructions
                                                                .GroupBy(x => x.Placeholder, StringComparer.Ordinal)
                                                                .ToDictionary(
                                                                    x => x.Key,
                                                                    x => rowContent.TryGetValue(x.First().Source.Name,
                                                                        out var value)
                                                                        ? value
                                                                        : string.Empty,
                                                                    StringComparer.Ordinal);
                                                        }),
                                                    ImageInstructions =
                                                        new Input<IReadOnlyDictionary<ImageSpecializedInstruction,
                                                            string>>(context =>
                                                            (IReadOnlyDictionary<ImageSpecializedInstruction, string>)
                                                            context.Get(downloadedImagePathsRefKey)!)
                                                }
                                            }
                                        }
                                    },
                                    new ReleaseEditingSlot(),
                                    new RemoveWorkingTemplateSlide(slideRegistry)
                                    {
                                        WorkingTemplateSlide =
                                            new Input<SlideIdentifier>(Utilities.GetRef(workingTemplateSlideRefKey))
                                    },
                                    new CleanupResources(workbookRegistry, slideRegistry)
                                    {
                                        Presentations = new Input<IReadOnlySet<PresentationIdentifier>>(context =>
                                            new HashSet<PresentationIdentifier>
                                            {
                                                new((string)context.Get(outputPathRefKey)!)
                                            })
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}