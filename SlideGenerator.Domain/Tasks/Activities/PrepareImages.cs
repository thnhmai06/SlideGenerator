using ClosedXML.Excel;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using SlideGenerator.Domain.Download.Models;
using SlideGenerator.Domain.Settings.Abstractions;
using SlideGenerator.Domain.Tasks.Models.Image;
using SlideGenerator.Framework.Sheet.Services;

namespace SlideGenerator.Domain.Tasks.Activities;

using ImageConfigs = IReadOnlyList<GeneralInstruction>;
using ImageFlatConfigs = IReadOnlyList<SpecializedInstruction>;
using RowData = (int Index, IReadOnlyDictionary<string, string>? Content);

/// <summary>
///     Workflow step that resolves and downloads images required for a row's image replacements.
/// </summary>
public class PrepareImages(DownloadRegistry downloadRegistry, ISettingProvider settingProvider) : WorkflowBase
{
    // -------------------------------------------------------------------------
    // Variables
    // -------------------------------------------------------------------------
    
    private readonly Variable<string> _downloadRootFolder = new("DownloadRootFolder", null!);
    private readonly Variable<string> _imagePath = new("ImagePath", null!);
    private readonly Variable<RowData> _imageRowData = new("ImageRowData", new RowData(0, null));
    private readonly Variable<ImageFlatConfigs> _flattenConfigs = new("FlattenConfigs", null!);
    
    // -------------------------------------------------------------------------
    // Inputs
    // -------------------------------------------------------------------------

    /// <summary>Input: Image replacement configurations for the current slide.</summary>
    public Input<ImageConfigs> ImageConfigs { get; set; } = null!;

    /// <summary>Input: Row content key-value pairs resolved from the workbook.</summary>
    public Input<RowData> RowData { get; set; } = null!;

    /// <summary>Input: Presentation file name used to build the download subfolder path.</summary>
    public Input<string> PresentationName { get; set; } = null!;
    
    // -------------------------------------------------------------------------
    // Build
    // -------------------------------------------------------------------------
    
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Variables = { _flattenConfigs, _imageRowData, _downloadRootFolder },
            Activities =
            {
                GetImageRowData,
                ResolveImageUrl,
                PrepareRootPath,
                new ParallelForEach<SpecializedInstruction>
                {
                    Items = new Input<object>(_flattenConfigs),
                    Body = new Sequence
                    {
                        Activities =
                        {
                            DownloadImage
                        },
                        Name = "HandleImage"
                    },
                    Name = "IterateImageContents"
                }
            },
            Name = "PrepareImages"
        };
    }

    // -------------------------------------------------------------------------
    // Activities
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Filters image configs by available row columns and extracts the relevant image URL values.
    /// </summary>
    private Inline GetImageRowData => new(context =>
    {
        var imageConfigs = context.Get(ImageConfigs);
        var rowData = context.Get(RowData);

        if (imageConfigs is null || rowData is { Content: null })
            return;

        var necessary =
            imageConfigs.Where(config => config.Sources.Any(col => rowData.Content.ContainsKey(col)));
        var flatten = necessary.SelectMany(config => config.Specialize()).ToList();
        _flattenConfigs.Set(context.ExpressionExecutionContext, flatten);

        var imageCols = flatten.Select(config => config.Source).ToHashSet();
        var imageRowData =
            rowData.Content
                .Where(kvp => imageCols.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        _imageRowData.Set(context.ExpressionExecutionContext, imageRowData);
    })
    {
        Name = "GetImageRowData"
    };

    private Inline ResolveImageUrl => new(context =>
    {
        var flattenConfigs = context.Get<ImageFlatConfigs>(_flattenConfigs);
        var imageRowData = context.Get<RowData>(_imageRowData);

        if (flattenConfigs is null || imageRowData is { Content: null })
            return;

        // TODO: Resolve them all by using ParallelForEach
    })
    {
        Name = "ResolveImageUrl"
    };

    /// <summary>
    ///     Builds the root download folder path: <c>{DownloadFolder}/{WorkbookName}/{PresentationName}</c>.
    /// </summary>
    private Inline PrepareRootPath => new(context =>
    {
        var presentationName = context.Get(PresentationName);
        var workbook = context.WorkflowExecutionContext.GetProperty<IXLWorkbook>("Workbooks");
        var downloadFolder = settingProvider.Current.Download.DownloadFolder;

        if (string.IsNullOrEmpty(presentationName) || workbook is null)
            return;

        var workbookName = workbook.GetName() ?? ".unknown";
        _downloadRootFolder.Set(
            context.ExpressionExecutionContext,
            Path.Combine(downloadFolder, workbookName, presentationName));
    })
    {
        Name = "PrepareRootPath"
    };

    private Inline DownloadImage => new(async context =>
    {
        var imageFlatConfig = context.GetVariable<SpecializedInstruction>("CurrentValue");
        var imageRowData = _imageRowData.Get(context.ExpressionExecutionContext);
        var downloadRootFolder = _downloadRootFolder.Get(context.ExpressionExecutionContext);

        if (imageFlatConfig is null || imageRowData is { Content: null } || string.IsNullOrEmpty(downloadRootFolder))
            return;

        // .../{Target}/{Source}/{Id}.ext
        var downloadLocation =
            Path.Combine(downloadRootFolder, imageFlatConfig.Target.ToString(), imageFlatConfig.Source);
        var downloadInfo = new DownloadRequest(
            imageRowData.Content[imageFlatConfig.Source],
            downloadLocation,
            imageRowData.Index.ToString());
        if (downloadRegistry.TryGetOrCreateDownloader(downloadInfo,
                settingProvider.Current.Download.GetConfigurationObject(), out var downloader))
            await downloader.Download();

        if (!DownloadRegistry.TryGetCompletedDownloadFilePath(downloadInfo, out var imagePath))
            return;

        _imagePath.Set(context.ExpressionExecutionContext, imagePath);
    })
    {
        Name = "DownloadImage"
    };
}