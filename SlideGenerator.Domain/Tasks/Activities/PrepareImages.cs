using ClosedXML.Excel;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using SlideGenerator.Domain.Download.Entities;
using SlideGenerator.Domain.Download.Services;
using SlideGenerator.Domain.Settings.Contacts;
using SlideGenerator.Domain.Tasks.Models;
using SlideGenerator.Framework.Sheet.Services;

namespace SlideGenerator.Domain.Tasks.Activities;

using ImageConfigs = IReadOnlyList<ImageConfig>;
using ImageFlatConfigs = IReadOnlyList<ImageFlatConfig>;
using RowData = (int Index, IReadOnlyDictionary<string, string>? Content);

/// <summary>
///     Workflow step that resolves and downloads images required for a row's image replacements.
/// </summary>
public class PrepareImages(DownloadManager downloadManager, ISettingProvider settingProvider) : WorkflowBase
{
    private readonly Variable<string> _downloadRootFolder = new("DownloadRootFolder", null!);
    private readonly Variable<string> _imagePath = new("ImagePath", null!);
    private readonly Variable<RowData> _imageRowData = new("ImageRowData", new RowData(0, null));

    // -------------------------------------------------------------------------
    // Variables
    // -------------------------------------------------------------------------

    private readonly Variable<ImageFlatConfigs> _necessaryFlattenConfigs = new("NecessaryFlattenConfigs", null!);
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
    // Activities
    // -------------------------------------------------------------------------

    // lowkenuinely magic-maxxing rn fr fr 🪄✨ this font is straight negative aura 📉 and rizz like a struggle 4
    // the famepilled npcs 🤖 but im an s-tier reader 🏆 if you cant decode this, youre literally unc-coded 👴
    // no cap, my brain is just built different 🧬 six-seven ✌️👅💦 ⁶🤷⁷

    // ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡿⠿⢛⣛⣩⣭⣭⣭⣭⣙⣩⣭⣭⣭⣭⣙⣛⠻⢿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡿⠟⣋⣵⣶⣿⣿⣿⣿⣿⠿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡷⣦⣙⠻⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡿⢋⣴⣿⣿⡟⡿⢻⣿⣿⡟⣿⠸⣿⡙⣿⣿⣇⢻⣿⣿⣿⣿⣿⣷⣍⡻⣷⣬⡻⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡿⢣⣶⢿⡿⢋⠍⢰⠃⣾⣿⣿⢡⣿⡆⢿⣧⠹⣿⣿⣆⢻⣿⣿⣿⣿⣿⣿⣿⣦⡹⣷⣌⠻⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠏⣴⡿⡡⠋⡴⢣⠢⠃⣼⣿⣿⢃⣾⣿⡇⣌⠻⣷⡈⠻⢿⣦⡙⢿⣿⣿⣿⣿⣿⣿⣷⣌⢿⣷⡜⢿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⢋⣼⡟⠁⠄⣨⠞⡁⢀⣾⣿⡿⢃⣾⣿⢏⣴⣿⣷⣮⣙⡂⠄⠨⢙⡂⠙⠻⢿⣿⣿⣿⣿⣿⣧⡙⢿⣆⢻⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡟⣸⣿⠃⣴⣶⣿⠖⣣⣾⣿⠟⣡⡾⠟⣫⣼⣿⣿⣿⣿⣿⣷⣶⣤⣼⣷⣶⣦⣬⣙⡻⢿⣿⣿⣿⣷⣜⠿⣎⢻⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⣿⣿⣿⣿⣿⣿⣿⣿⣿⡿⢠⣿⣯⣼⡿⢟⣡⣾⠿⢛⣡⣤⣴⣶⣿⣿⣿⣿⣿⣿⣿⣿⢿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣬⡙⣿⣿⣿⣷⣶⣆⠹⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⣿⣿⣿⣿⣿⣿⣿⣿⣿⠃⣾⣿⣿⣦⣤⣤⢀⣶⣾⢛⡭⠐⠒⠒⠬⡛⢿⣿⣿⢸⣿⣿⡌⣿⣿⡿⢋⠅⠒⠐⠒⢬⡝⢿⣷⡘⣿⣿⣿⣿⣿⣷⡜⢿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⣿⣿⣿⣿⣿⣿⣿⣿⣿⢸⣿⣿⣿⣿⣿⢃⣾⣿⡡⡏⠀⠐⠀⠂⠀⣈⣼⣿⡇⢾⣿⣿⡆⢿⣿⣧⣀⠀⠰⠠⠅⠀⢹⡎⣿⣷⡘⣿⣿⣿⣿⣿⡿⠷⠬⢙⢻⢿⣿⣿⣿⣿⣿
    // ⣿⣿⣿⣿⣿⣿⣿⣿⣿⢸⣿⣿⣿⣿⡏⣼⣿⣿⣿⡿⠶⠶⠶⢞⣫⣼⣿⠟⣡⣭⣶⣦⣽⣌⠻⣿⣦⣙⡲⠶⠶⠶⢿⣿⣿⣿⣧⠸⣿⣿⣿⣿⣿⣦⣤⣭⡭⢀⣿⣿⣿⣿⣿
    // ⣿⣿⡟⢻⣿⣿⣿⣿⡏⣸⣿⣿⣿⡿⢠⣿⣿⣿⣿⣿⣿⣿⣿⣿⠟⢋⣴⡿⠓⠹⣿⡏⠙⠿⢷⣎⢻⡻⣿⣿⣿⣿⣿⣿⣿⣿⣿⡆⢻⣿⣿⣿⣿⣿⡟⢉⣒⡁⣼⣿⣿⣿⡿
    // ⣿⣿⣷⠠⣉⡛⠿⢛⣠⣿⣿⣿⣿⡇⣿⣿⣿⣿⣿⣿⣿⡿⢋⣵⣿⣦⣛⣠⣴⣾⣿⣷⣶⣤⣛⣛⣼⣿⣦⣙⢿⣿⣿⣿⣿⣿⣿⣷⢸⣿⣿⣿⣿⣿⣿⣿⡿⢡⣿⣿⣿⡿⠞
    // ⣿⠋⢛⠷⠍⠛⢻⣿⣿⣿⣿⣿⣿⢰⣿⣿⣿⣿⣿⡿⣫⣶⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣍⢻⣿⣿⣿⣿⣿⢸⣿⣿⣿⣿⣿⣿⡿⣡⣾⣿⣿⣿⣿⡌
    // ⣿⠀⠦⡻⢿⣿⣿⣿⣿⣿⣿⣿⣟⢸⣿⣿⣿⣿⢏⣴⣿⣿⣿⠿⠟⠛⣛⡛⠉⠉⣉⠉⠉⢛⣛⠛⡛⠿⠿⣿⣿⣿⣷⡹⣿⣿⣿⣿⢸⣿⣿⣿⣿⠟⣫⠴⠟⣻⣿⣿⣿⡿⠁
    // ⣿⣆⠳⣦⣤⣽⣿⣿⣿⣿⣿⣿⣗⢸⣿⣿⣿⠇⣾⣿⡟⠋⠀⣬⢡⣶⣎⣰⣿⣿⣀⣾⣿⣷⣱⣶⡎⣥⡔⡂⢍⢿⣿⣷⢹⣿⣿⡟⢸⣿⣿⠿⣷⡶⠖⣫⣴⣿⡿⠏⠁⠀⠒
    // ⣿⣿⠣⠜⠻⢿⣿⣿⣿⣿⣿⣿⣿⠸⣿⣿⡏⢸⣿⡏⢠⢸⢇⣿⢻⣿⣿⣿⣿⣿⡛⣿⣿⣿⢿⣿⠿⣿⣇⣿⢈⠂⢹⣿⡌⣿⣿⡇⣿⡿⠛⠦⠄⣀⣿⡿⠉⠁⠀⠀⠀⠀⠀
    // ⣿⣿⣷⣬⡐⠻⢿⢿⣿⣿⣿⣿⣿⡆⢻⣿⣿⢸⣿⠀⢆⢆⠣⠍⠀⠀⠀⠈⠉⠀⠀⠀⠀⠀⠀⠀⠀⠀⢒⣋⠟⡄⠈⣿⡇⣿⡿⢰⡿⢁⣶⣤⣍⠻⠁⠀⠀⠀⠀⣀⣠⣀⣀
    // ⣿⣿⣿⣿⣷⣀⠨⢼⣿⣿⣿⣿⣿⣧⠸⣿⣿⢸⣿⠀⠘⠊⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠉⠐⠁⠀⣿⢣⣿⠇⡼⢃⣼⣿⣿⣿⣦⡐⢶⣶⣴⣶⣿⣿⣿⣿
    // ⣿⣿⣿⣿⣿⣿⣷⣶⣤⣭⣭⣄⠙⢿⣆⢻⣿⡌⣿⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣯⣼⡿⢀⣴⣿⣿⣿⣿⡏⢿⣷⣄⡙⢾⣿⣿⣿⣿⣿
    // ⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠟⢠⡆⢲⣬⡈⣿⣿⣿⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⣿⣿⡇⣾⣿⣿⣿⣿⣿⣿⢸⣿⣿⣿⡄⢿⣿⣿⣿⣿
    // ⣿⣿⣿⣿⣿⣿⣿⣿⠟⣠⣾⣿⣷⠸⣿⡇⢻⣿⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣸⣿⣿⢠⣿⣿⣿⣿⣿⣿⡏⣼⣿⣿⣿⡇⠸⣿⣿⣿⣿
    // ⣿⣿⣿⣿⣿⣿⡿⠛⠰⣿⣿⣿⣿⣆⢹⣿⠸⣿⣿⣿⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⣿⡟⣸⣿⣿⣿⣿⣿⡟⣰⣿⣿⣿⡿⢁⣷⣤⡹⣿⣿
    // ⣿⣿⣿⣿⡟⢉⣴⣾⣆⠹⣿⣿⣿⣿⣆⠻⡆⣿⣿⣿⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢠⣿⣿⠇⣿⣿⣿⣿⡿⢋⣴⣿⣿⣿⠟⣠⣾⣿⣿⣷⡌⢿
    // ⣿⣿⣿⠋⣴⣿⣿⣿⣿⣷⡈⠻⣿⣿⣿⣿⣧⢸⣿⣿⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⣿⣿⢰⣿⣿⣿⣿⣷⣿⣿⣿⡿⢃⣴⣿⣿⣿⣿⣿⣿⡌
    // ⣿⣿⢃⣾⣿⣿⣿⣿⣿⣿⣿⣦⡈⠻⣿⣿⣿⡈⣿⣿⣧⢠⢤⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠴⠆⣾⣿⡏⣸⣿⣿⣿⣿⣿⣿⡿⢋⣴⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⣿⠇⣾⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣶⣌⠻⢿⡇⢿⣿⣿⠰⢿⡂⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣿⠇⣿⣿⡇⣿⣿⣿⣿⣿⡿⢋⣴⣾⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⡿⢸⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣶⣦⢸⣿⣿⡄⢿⣟⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣛⠻⢸⣿⣿⢁⣿⣿⣿⠟⣁⣴⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⡇⣾⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡀⣿⣿⣇⠙⣫⣤⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠸⠿⡇⣸⣿⡟⢸⠿⢋⣥⣾⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⠇⣿⣿⣿⣿⣿⣆⠸⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡇⢻⣿⣿⡄⠿⣯⡄⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⡻⣿⢀⣿⣿⡏⣠⣾⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⠀⣿⣿⣿⣿⣿⣿⡀⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⢸⣿⣿⣇⢰⣿⢗⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢴⣧⠙⢸⣿⣿⠃⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⠰⣿⣿⣿⣿⣿⣿⣧⠸⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡈⣿⣿⣿⣆⢣⣿⢃⡀⠀⠀⠀⠀⠀⠀⣀⢰⣧⠻⢡⣿⣿⣿⢰⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⠘⣿⣿⣿⣿⣿⣿⣿⡆⢻⣿⣿⣿⣿⣿⣿⣿⣿⣿⡇⢿⣿⣯⠻⣦⠉⢿⡇⣿⡷⣶⢲⣿⡞⣿⠺⠟⣠⢿⣿⣿⡇⣸⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⠈⣿⣿⣿⣿⣿⣿⣿⣿⡄⢻⣿⣿⣿⣿⣿⣿⣿⣿⣿⠸⣿⣿⣷⡙⢷⣦⣔⣈⠉⠛⠩⠛⢁⣉⣴⡾⢋⣼⣿⣟⢀⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⡄⣿⣿⣿⣿⣿⣿⣿⣿⣿⡄⢿⣿⣿⣿⣿⣿⣿⣿⣿⡆⢻⣿⣿⣿⣷⣭⣛⠻⠿⠿⠿⠿⠿⢛⣫⣴⣿⣿⣿⠇⣼⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿
    // ⡇⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡄⠹⣿⣿⣿⣿⣿⣿⣿⣿⡌⠿⣿⣿⣿⣿⣿⡿⣿⣿⣿⣿⢿⢿⣿⣿⣿⡿⠏⣼⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿

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
            imageConfigs.Where(config => config.Columns.Any(col => rowData.Content.ContainsKey(col)));
        var flatten = necessary.SelectMany(config => config.Flatten()).ToList();
        _necessaryFlattenConfigs.Set(context.ExpressionExecutionContext, flatten);

        var imageCols = flatten.Select(config => config.Column).ToHashSet();
        var imageRowData =
            rowData.Content
                .Where(kvp => imageCols.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        _imageRowData.Set(context.ExpressionExecutionContext, imageRowData);
    })
    {
        Name = "GetImageRowData"
    };

    private Inline ResolveImageUrls => new(context =>
    {
        var flattenConfigs = context.Get<ImageFlatConfigs>(_necessaryFlattenConfigs);
        var imageRowData = context.Get<RowData>(_imageRowData);

        if (flattenConfigs is null || imageRowData is { Content: null })
            return;

        // TODO: Resolve them all by using ParallelForEach
    })
    {
        Name = "ResolveImageUrls"
    };

    /// <summary>
    ///     Builds the root download folder path: <c>{DownloadFolder}/{WorkbookName}/{PresentationName}</c>.
    /// </summary>
    private Inline PrepareRootPath => new(context =>
    {
        var presentationName = context.Get(PresentationName);
        var workbook = context.WorkflowExecutionContext.GetProperty<IXLWorkbook>("Workbook");
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
        var imageFlatConfig = context.GetVariable<ImageFlatConfig>("CurrentValue");
        var imageRowData = _imageRowData.Get(context.ExpressionExecutionContext);
        var downloadRootFolder = _downloadRootFolder.Get(context.ExpressionExecutionContext);

        if (imageFlatConfig is null || imageRowData is { Content: null } || string.IsNullOrEmpty(downloadRootFolder))
            return;

        // .../{ShapeId}/{Column}/{Index}.ext
        var downloadLocation =
            Path.Combine(downloadRootFolder, imageFlatConfig.ShapeId.ToString(), imageFlatConfig.Column);
        var downloadInfo = new DownloadInfo(
            imageRowData.Content[imageFlatConfig.Column],
            downloadLocation,
            imageRowData.Index.ToString());
        if (downloadManager.TryGetOrCreateDownloader(downloadInfo,
                settingProvider.Current.Download.GetConfigurationObject(), out var downloader))
            await downloader.Download();

        if (!DownloadManager.IsDownloadCompleted(downloadInfo, out var imagePath))
            return;

        _imagePath.Set(context.ExpressionExecutionContext, imagePath);
    })
    {
        Name = "DownloadImage"
    };

    // -------------------------------------------------------------------------
    // Build
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Variables = { _necessaryFlattenConfigs, _imageRowData, _downloadRootFolder },
            Activities =
            {
                GetImageRowData,
                ResolveImageUrls,
                PrepareRootPath,
                new ParallelForEach<ImageFlatConfig>
                {
                    Items = new Input<object>(_necessaryFlattenConfigs),
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
}