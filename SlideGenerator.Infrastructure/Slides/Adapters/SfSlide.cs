using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Entities.Shape;
using SlideGenerator.Domain.Slides.Entities.Slide;
using SlideGenerator.Domain.Slides.Models.Previews;
using SfPresNs = Syncfusion.Presentation;
using SfRenderer = Syncfusion.PresentationRenderer;

namespace SlideGenerator.Infrastructure.Slides.Adapters;

/// <summary>
///     Represents a slide within a Syncfusion-backed presentation.
/// </summary>
public sealed class SfSlide : ISlide
{
    /// <summary>
    ///     The parent presentation adapter.
    /// </summary>
    private readonly SfPresentation _presentation;

    internal SfSlide(SfPresentation presentation, SfPresNs.ISlide core, int index)
    {
        _presentation = presentation;
        Core = core;
        Index = index;
    }

    /// <summary>The underlying Syncfusion slide.</summary>
    internal SfPresNs.ISlide Core { get; }

    /// <summary>1-based position of this slide within the presentation.</summary>
    public int Index { get; }

    /// <inheritdoc />
    public uint Id => (uint)Index;

    /// <inheritdoc />
    public string? Name => Core.Name;

    /// <inheritdoc />
    public IPresentation Presentation => _presentation;

    /// <inheritdoc />
    public IEnumerable<IShape> DescendShapes()
    {
        // ISlide.Shapes returns ISlideItem; IShape and IPicture are subtypes of ISlideItem
        return Core.Shapes
            .OfType<SfPresNs.IShape>()
            .Select(s => new SfShape(this, s));
    }

    /// <inheritdoc />
    public async Task<SlidePreview> GetPreview(bool skipPreview = false, CancellationToken ct = default)
    {
        if (skipPreview) return new SlidePreview(Index, []);

        var filePath = _presentation.Identifier.FilePath;
        await using var fs = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
            bufferSize: 4096, useAsync: true);

        using var pptxDoc = SfPresNs.Presentation.Open(fs);
        pptxDoc.PresentationRenderer = new SfRenderer.PresentationRenderer();

        await using var imgStream = pptxDoc.Slides[Index - 1].ConvertToImage(SfPresNs.ExportImageFormat.Png);
        using var ms = new MemoryStream();
        await imgStream.CopyToAsync(ms, ct).ConfigureAwait(false);
        return new SlidePreview(Index, ms.ToArray());
    }
}
