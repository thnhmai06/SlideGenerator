using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using SlideGenerator.Application.Common;
using SlideGenerator.Application.Slide.Abstractions;
using SlideGenerator.Application.System;
using SlideGenerator.Application.Tasks.Generation.Activities;
using SlideGenerator.Domain.Cloud.Abstractions;
using SlideGenerator.Domain.Download.Abstractions;
using SlideGenerator.Domain.Settings.Interfaces;
using SlideGenerator.Domain.Sheet.Entities;
using SlideGenerator.Domain.Sheet.Models;
using SlideGenerator.Domain.Slide.Entities;
using SlideGenerator.Domain.Slide.Models;
using SlideGenerator.Domain.Tasks.Models;
using TextSpecializedInstruction = SlideGenerator.Domain.Tasks.Models.Text.SpecializedInstruction;
using ImageSpecializedInstruction = SlideGenerator.Domain.Tasks.Models.Image.SpecializedInstruction;

namespace SlideGenerator.Application.Tasks.Generation;

public sealed class GenerationWorkflow(
    IRegistry<IReadOnlyWorkbook> workbookRegistry,
    IRegistry<IPresentation> slideRegistry,
    ISlideContentOperator slideContentOperator,
    IFileSystem fileSystem,
    ICloudResolver cloudResolver,
    IDownloadRegistry downloadRegistry,
    ISettingProvider settingProvider) : WorkflowBase
{
    private const string RequestRefKey = GenerationRequest.Name;
    private const int WorkingTemplateSlideIndex = 1;

    private static GenerationRequest? GetRequest(ExpressionExecutionContext context)
    {
        return (GenerationRequest?)context.Get(RequestRefKey);
    }

    protected override void Build(IWorkflowBuilder builder)
    {
        _ = builder.WithInput<GenerationRequest>(RequestRefKey, GenerationRequest.Description);

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
                    Items = new(context => GetRequest(context)!.Graph.Keys.ToList()),
                    Body = new Sequence
                    {
                        Name = "GenerateByWorksheet",
                        Activities =
                        {
                            new BuildOutputPath(workbookRegistry)
                            {
                                SaveFolder = new(context => GetRequest(context)!.SaveFolder),
                                Worksheet = new(context => context.GetVariable<WorksheetIdentifier>("CurrentValue")!),
                                Extension = new(context => GetRequest(context)!.OutputExtension),
                                OutputPath = new(Utilities.GetRef(outputPathRefKey))
                            },
                            new CreateWorkingPresentation(slideRegistry, workbookRegistry, fileSystem)
                            {
                                TemplateSlide = new(context =>
                                {
                                    var request = GetRequest(context)!;
                                    var worksheet = context.GetVariable<WorksheetIdentifier>("CurrentValue")!;
                                    return request.Graph[worksheet];
                                }),
                                Worksheet = new(context => context.GetVariable<WorksheetIdentifier>("CurrentValue")!),
                                OutputPath = new(Utilities.GetRef(outputPathRefKey)),
                                WorkingTemplateSlide = new(Utilities.GetRef(workingTemplateSlideRefKey))
                            },
                            new ScanTemplateContent(slideRegistry, slideContentOperator)
                            {
                                TemplateSlide = new(Utilities.GetRef(workingTemplateSlideRefKey)),
                                Placeholders = new(Utilities.GetRef(templatePlaceholdersRefKey)),
                                ImageShapeIds = new(Utilities.GetRef(templateImageShapeIdsRefKey))
                            },
                            new SpecializeInstructions(workbookRegistry)
                            {
                                Worksheet = new(context => context.GetVariable<WorksheetIdentifier>("CurrentValue")!),
                                TemplateSlide = new(Utilities.GetRef(workingTemplateSlideRefKey)),
                                RawTextInstructions = new(context =>
                                    GetRequest(context)!.TextInstructions),
                                RawImageInstructions = new(context =>
                                    GetRequest(context)!.ImageInstructions),
                                TemplatePlaceholders = new(Utilities.GetRef(templatePlaceholdersRefKey)),
                                TemplateImageShapeIds = new(Utilities.GetRef(templateImageShapeIdsRefKey)),
                                TextInstructions = new(Utilities.GetRef(specializedTextInstructionsRefKey)),
                                ImageInstructions = new(Utilities.GetRef(specializedImageInstructionsRefKey))
                            },
                            new ResolveImageUrls(cloudResolver, workbookRegistry)
                            {
                                ImageInstructions = new(Utilities.GetRef(specializedImageInstructionsRefKey)),
                                RowIndex = new(1),
                                WorksheetInfo =
                                    new(context => context.GetVariable<WorksheetIdentifier>("CurrentValue")!),
                                ResolvedImageUrls = new(Utilities.GetRef(resolvedImageUrlsRefKey))
                            },
                            new DownloadImages(downloadRegistry, settingProvider)
                            {
                                ImageUrls = new(Utilities.GetRef(resolvedImageUrlsRefKey)),
                                Worksheet = new(context => context.GetVariable<WorksheetIdentifier>("CurrentValue")!),
                                RowIndex = new(1),
                                ImagePaths = new(Utilities.GetRef(downloadedImagePathsRefKey))
                            },
                            new EditImages
                            {
                                DownloadedImagePaths = new(Utilities.GetRef(downloadedImagePathsRefKey))
                            },
                            new ForEach<int>
                            {
                                Name = "GenerateSlidesByRecord",
                                Items = new(context =>
                                {
                                    var worksheet = context.GetVariable<WorksheetIdentifier>("CurrentValue")!;
                                    var workbook = workbookRegistry.GetOrOpen(worksheet.Workbook.FilePath,
                                        isEditable: false);

                                    if (!workbook.TryGetWorksheet(worksheet.Name, out var readOnlyWorksheet))
                                        throw new InvalidOperationException(
                                            $"Worksheet '{worksheet.Name}' does not exist in workbook.");

                                    var rowCount = readOnlyWorksheet.GetRowsCount();
                                    return Enumerable.Range(1, rowCount).ToList();
                                }),
                                CurrentValue = new(Utilities.GetRef(currentRowIndexRefKey)),
                                Body = new Sequence
                                {
                                    Name = "CloneThenReplaceByRecord",
                                    Activities =
                                    {
                                        new CloneTemplateSlide(slideRegistry)
                                        {
                                            TemplateSlide = new(Utilities.GetRef(workingTemplateSlideRefKey)),
                                            InsertAtIndex = new(context =>
                                            {
                                                var rowIndex =
                                                    context.Get<int>(Utilities.GetRef(currentRowIndexRefKey));
                                                return WorkingTemplateSlideIndex + rowIndex;
                                            })
                                        },
                                        new ReplaceSlideContents(slideRegistry, slideContentOperator)
                                        {
                                            SlideIdentifier = new(context =>
                                            {
                                                var rowIndex =
                                                    context.Get<int>(Utilities.GetRef(currentRowIndexRefKey));
                                                var workingTemplateSlide =
                                                    context.Get<SlideIdentifier>(
                                                        Utilities.GetRef(workingTemplateSlideRefKey))!;
                                                return workingTemplateSlide.Presentation.GetSlide(
                                                    WorkingTemplateSlideIndex + rowIndex);
                                            }),
                                            TextInstructions = new(context =>
                                            {
                                                var worksheet =
                                                    context.GetVariable<WorksheetIdentifier>("CurrentValue")!;
                                                var rowIndex =
                                                    context.Get<int>(Utilities.GetRef(currentRowIndexRefKey));
                                                var workbook = workbookRegistry.GetOrOpen(worksheet.Workbook.FilePath,
                                                    isEditable: false);

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
                                                        x => rowContent.TryGetValue(x.First().Source.ColumnName,
                                                            out var value)
                                                            ? value
                                                            : string.Empty,
                                                        StringComparer.Ordinal);
                                            }),
                                            ImageInstructions = new(context =>
                                            {
                                                var imagePaths =
                                                    (IReadOnlyDictionary<ImageSpecializedInstruction, string>)context
                                                        .Get(downloadedImagePathsRefKey)!;
                                                return imagePaths
                                                    .GroupBy(x => x.Key.Target.Id)
                                                    .ToDictionary(x => x.Key, x => x.First().Value);
                                            })
                                        },
                                    }
                                }
                            },
                            new RemoveWorkingTemplateSlide(slideRegistry)
                            {
                                WorkingTemplateSlide = new(Utilities.GetRef(workingTemplateSlideRefKey))
                            },
                            new CleanupResources(workbookRegistry, slideRegistry)
                            {
                                Presentations =
                                    new(context =>
                                        new HashSet<PresentationIdentifier>
                                        {
                                            new((string)context.Get(outputPathRefKey)!)
                                        })
                            }
                        }
                    }
                },
                new CleanupResources(workbookRegistry, slideRegistry)
                {
                    Workbooks = new(context =>
                        GetRequest(context)!
                            .Graph.Keys
                            .Select(x => x.Workbook)
                            .Distinct()
                            .ToHashSet())
                }
            }
        };
    }
}