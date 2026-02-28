using System.Drawing;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using ImageMagick;
using SlideGenerator.Features.Configs.Contracts;
using SlideGenerator.Framework.Image.Factory;
using SlideGenerator.Framework.Image.Models.FaceDetection;
using SlideGenerator.Framework.Image.Models.Roi;
using SlideGenerator.Framework.Image.Services;
using SlideGenerator.Framework.Sheet.Services;
using SlideGenerator.Framework.Slide.Services;
using SlideGenerator.Services.Generating.Models.Configs;

namespace SlideGenerator.Services.Generating.Services;

//TODO: Kiem tra service nay

/// <summary>
///     Executes per-row slide generation operations, including text and image replacement.
/// </summary>
/// <remarks>
///     Initializes runtime dependencies used for generation.
/// </remarks>
/// <param name="faceDetectorManager">Face detector manager.</param>
/// <param name="downloadService">Download service.</param>
/// <param name="configProvider">Read-only configuration manager.</param>
public sealed class GenerateService(
    FaceDetectorModelManager faceDetectorManager,
    DownloadService downloadService,
    IConfigProvider configProvider) : IAsyncDisposable
{
    /// <summary>
    ///     Configuration provider for runtime options.
    /// </summary>
    private readonly IConfigProvider _configProvider = configProvider;

    /// <summary>
    ///     Service for downloading remote image sources.
    /// </summary>
    private readonly DownloadService _downloadService = downloadService;

    /// <summary>
    ///     Face detector manager used to select and control detector model lifecycle.
    /// </summary>
    private readonly FaceDetectorModelManager _faceDetectorManager = faceDetectorManager;

    /// <summary>
    ///     Disposes runtime resources allocated for generation.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        return _faceDetectorManager.DisposeAsync();
    }

    /// <summary>
    ///     Initializes face detector model by key.
    /// </summary>
    /// <param name="modelKey">Model key to initialize.</param>
    public Task<bool> InitializeFaceDetectorAsync(FaceDetectorModelKey modelKey)
    {
        return _faceDetectorManager.InitializeAsync(modelKey);
    }

    /// <summary>
    ///     De-initializes face detector model by key.
    /// </summary>
    /// <param name="modelKey">Model key to de-initialize.</param>
    public Task<bool> DeInitializeFaceDetectorAsync(FaceDetectorModelKey modelKey)
    {
        return _faceDetectorManager.DeInitializeAsync(modelKey);
    }

    /// <summary>
    ///     Selects a face detector model manually.
    /// </summary>
    public Task SelectFaceDetectorModelAsync(FaceDetectorModelKey modelKey)
    {
        _faceDetectorManager.SelectModel(modelKey);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Clones a template slide and applies row-bound text and image data to the new slide.
    /// </summary>
    /// <param name="document">Target presentation document.</param>
    /// <param name="relationshipId">Template slide relationship identifier.</param>
    /// <param name="usedRange">Source worksheet data range.</param>
    /// <param name="rowIndex">1-based row index in worksheet data body.</param>
    /// <param name="textConfig">Text binding configuration list.</param>
    /// <param name="imageConfig">Image binding configuration list.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ProcessRowAsync(
        PresentationDocument document,
        string relationshipId,
        IXLRange? usedRange,
        int rowIndex,
        IReadOnlyList<TextConfig> textConfig,
        IReadOnlyList<ImageConfig> imageConfig,
        CancellationToken cancellationToken)
    {
        var data = GetRowData(usedRange, rowIndex);
        var newSlide = XmlPresentationService.CloneSlide(document, relationshipId);
        await ApplyTextAsync(newSlide, data, textConfig);
        await ApplyImageAsync(newSlide, data, imageConfig, cancellationToken);
    }

    private static IReadOnlyDictionary<string, string> GetRowData(IXLRange? usedRange, int rowIndex)
    {
        return usedRange == null
            ? new Dictionary<string, string>()
            : WorksheetService.GetRowContent(usedRange, rowIndex);
    }

    private static async Task ApplyTextAsync(
        SlidePart slidePart,
        IReadOnlyDictionary<string, string> rowData,
        IReadOnlyList<TextConfig> config)
    {
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var binding in config)
        foreach (var column in binding.Columns)
        {
            if (!rowData.TryGetValue(column, out var value) || string.IsNullOrWhiteSpace(value)) continue;
            replacements[binding.Placeholder] = value;
            break;
        }

        if (replacements.Count == 0) return;
        await TextReplacer.ReplaceTextAsync(slidePart, replacements);
    }

    private async Task ApplyImageAsync(
        SlidePart slidePart,
        IReadOnlyDictionary<string, string> rowData,
        IReadOnlyList<ImageConfig> imageBindings,
        CancellationToken cancellationToken)
    {
        foreach (var binding in imageBindings)
        {
            var source = ResolveValue(rowData, binding.Columns);
            if (string.IsNullOrWhiteSpace(source)) continue;

            var sourceBytes = await LoadImageBytesAsync(source, cancellationToken);
            if (sourceBytes.Length == 0) continue;

            var picture = ShapeService.FindPictureById(slidePart, binding.ShapeId);
            if (picture != null)
            {
                var targetSize = ShapeService.GetPictureSize(picture);
                var imageBytes =
                    await ProcessImageBytesAsync(sourceBytes, targetSize, binding.RoiType, cancellationToken);
                using var stream = new MemoryStream(imageBytes, false);
                ImageReplacer.ReplaceImage(slidePart, picture, stream);
                continue;
            }

            var shape = ShapeService.FindShapeById(slidePart, binding.ShapeId);
            if (shape != null)
            {
                var targetSize = ShapeService.GetShapeSize(shape);
                var imageBytes =
                    await ProcessImageBytesAsync(sourceBytes, targetSize, binding.RoiType, cancellationToken);
                using var stream = new MemoryStream(imageBytes, false);
                ImageReplacer.ReplaceImage(slidePart, shape, stream);
            }
        }
    }

    private async Task<byte[]> ProcessImageBytesAsync(
        byte[] sourceBytes,
        Size targetSize,
        RoiType roiType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var image = new MagickImage(sourceBytes);
        var mat = ConvertingService.ConvertImageToMat(image);
        if (mat == null || mat.IsEmpty) return sourceBytes;

        try
        {
            var roiOptions = ;
            var roiFactory = new RoiFactory { Options = roiOptions };
            if (roiType == RoiType.RuleOfThirds)
            {
                var faceDetector = await _faceDetectorManager
                    .GetCurrentModelAsync()
                    .ConfigureAwait(false);

                roiFactory = new RoiFactory
                {
                    Options = roiOptions,
                    FaceDetector = faceDetector
                };
            }

            var calculator = roiFactory.GetCalculator(roiType);
            var cropRect = await calculator.CalculateRoiAsync(mat, targetSize).ConfigureAwait(false);
            ManipulatingService.Crop(ref mat, cropRect);
            ManipulatingService.Resize(ref mat, targetSize);
            return ConvertingService.ConvertMatToImage(mat);
        }
        finally
        {
            mat.Dispose();
        }
    }

    private async Task<byte[]> LoadImageBytesAsync(string source, CancellationToken cancellationToken)
    {
        if (Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return await _downloadService.DownloadAsync(uri, cancellationToken);

        var filePath = Path.GetFullPath(source);
        return File.Exists(filePath) ? await File.ReadAllBytesAsync(filePath, cancellationToken) : [];
    }

    private static string ResolveValue(
        IReadOnlyDictionary<string, string> rowData,
        IReadOnlyList<string> columns)
    {
        foreach (var column in columns)
        {
            if (string.IsNullOrWhiteSpace(column)) continue;
            if (!rowData.TryGetValue(column, out var value)) continue;
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return string.Empty;
    }
}