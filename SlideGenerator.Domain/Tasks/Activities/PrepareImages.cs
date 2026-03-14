using ClosedXML.Excel;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using SlideGenerator.Domain.Download.Services;
using SlideGenerator.Domain.Settings.Contacts;
using SlideGenerator.Domain.Tasks.Models;
using SlideGenerator.Framework.Sheet.Services;

namespace SlideGenerator.Domain.Tasks.Activities;

using RowContent = IReadOnlyDictionary<string, string>;

public class PrepareImages(DownloadManager downloadManager, ISettingProvider settingProvider) : WorkflowBase
{
    public Input<IReadOnlyList<ImageConfig>> ImageConfigs { get; set; } = null!;

    public Input<RowContent> RowContent { get; set; } = null!;

    public Input<string> PresentationName { get; set; } = null!;

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence()
        {
            Activities =
            {
                GetImageContents,
                DownloadImage
            },
            Name = "PrepareImages"
        };
    }

    private Inline GetImageContents => new(context =>
    {
        var imageConfigs = context.Get(ImageConfigs);
        var rowContent = context.Get(RowContent);

        if (imageConfigs is null || rowContent is null)
            return;

        var configColumns = imageConfigs.SelectMany(config => config.Columns);
        var contentColumns = rowContent.Keys;

        var imageColumns = configColumns.Intersect(contentColumns).ToHashSet();
        var imageContents = rowContent
            .Where(kvp => imageColumns.Contains(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        context.WorkflowExecutionContext.Properties["ImageContents"] = imageContents;
    })
    {
        Name = "GetImageContents"
    };

    private Inline DownloadImage => new(context =>
    {
        var imageContents = context.WorkflowExecutionContext.GetProperty<RowContent>("ImageContents");
        var presentationName = context.Get(PresentationName);
        var workbook = context.WorkflowExecutionContext.GetProperty<IXLWorkbook>("Workbook");
        var downloadFolder = settingProvider.Current.Download.DownloadFolder;

        if (imageContents is null || string.IsNullOrEmpty(presentationName) ||
            workbook is null)
            return;

        // Download Location = {DownloadFolder}/{WorkbookName}/{PresentationName}/{ShapeID}/{RowIndex}.ext
        var workbookName = workbook.GetName() ?? ".unknown";
        // TODO: complete save folder and download
    })
    {
        Name = "DownloadImage"
    };
}