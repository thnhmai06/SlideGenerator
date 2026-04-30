using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using SlideGenerator.Domain.Slides.Entities.Shape;
using SlideGenerator.Domain.Slides.Entities.Slide;
using SlideGenerator.Domain.Slides.Models.Previews;
using SfPresNs = Syncfusion.Presentation;

namespace SlideGenerator.Infrastructure.Slides.Adapters;

/// <summary>
///     Represents a shape within a Syncfusion-backed slide.
///     Wraps <see cref="SfPresNs.IShape" /> — which also covers <see cref="SfPresNs.IPicture" /> shapes.
/// </summary>
public sealed class SfShape : IShape
{
    private readonly SfSlide _slide;

    internal SfShape(SfSlide slide, SfPresNs.IShape core)
    {
        _slide = slide;
        Core = core;
    }

    /// <summary>The underlying Syncfusion shape.</summary>
    internal SfPresNs.IShape Core { get; }

    /// <inheritdoc />
    public ISlide Slide => _slide;

    /// <inheritdoc />
    /// <remarks>Syncfusion IShape does not expose a numeric shape ID; using name hash as stable proxy.</remarks>
    public uint Id => (uint)(Core.ShapeName?.GetHashCode() ?? 0);

    /// <inheritdoc />
    public string Name => Core.ShapeName ?? string.Empty;

    /// <inheritdoc />
    /// <remarks>Syncfusion returns positions as <c>double</c> in EMU. Converted to pixels at 96 DPI.</remarks>
    public RectangleF Bounds => new(
        (float)(Core.Left / Utilities.EmuPerPixel),
        (float)(Core.Top / Utilities.EmuPerPixel),
        (float)(Core.Width / Utilities.EmuPerPixel),
        (float)(Core.Height / Utilities.EmuPerPixel));

    /// <inheritdoc />
    public string? TextContent => Core.TextBody?.Text;

    /// <inheritdoc />
    public bool IsPicture => Core is SfPresNs.IPicture;

    /// <inheritdoc />
    /// <remarks>
    ///     BlipFill equivalent in Syncfusion: a non-picture shape whose fill is a picture fill.
    /// </remarks>
    public bool HasBlipFill =>
        Core is not SfPresNs.IPicture &&
        Core.Fill.FillType == SfPresNs.FillType.Picture;

    /// <inheritdoc />
    public bool TryGetPicture([MaybeNullWhen(false)] out byte[] image)
    {
        if (Core is not SfPresNs.IPicture picture)
        {
            image = null;
            return false;
        }

        image = picture.ImageData;
        return image is { Length: > 0 };
    }

    /// <inheritdoc />
    public bool TryGetBlipFill([MaybeNullWhen(false)] out byte[] image)
    {
        if (!HasBlipFill)
        {
            image = null;
            return false;
        }

        var picFill = Core.Fill.PictureFill;
        image = picFill?.ImageBytes;
        return image is { Length: > 0 };
    }

    /// <inheritdoc />
    public ShapePreview GetPreview()
    {
        byte[] image = [];
        if (TryGetPicture(out var pic)) image = pic;
        else if (TryGetBlipFill(out var fill)) image = fill;
        return new ShapePreview(Id, Name, Bounds, image);
    }
}
