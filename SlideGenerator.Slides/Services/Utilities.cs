using System.Drawing;
using Syncfusion.Presentation;
using Syncfusion.PresentationRenderer;

namespace SlideGenerator.Slides.Services;

public static class Utilities
{
    private const double EmuPerPixel = 9525.0;

    public static byte[] GetPreview(this ISlide slide)
    {
        var renderer = new PresentationRenderer();
        using var stream = slide.ConvertToImage(ExportImageFormat.Png);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    extension(IShape shape)
    {
        public byte[] GetPreview()
        {
            if (shape is IPicture picture)
                return picture.ImageData;

            if (shape.Fill.FillType == FillType.Picture)
                return shape.Fill.PictureFill.ImageBytes;

            return [];
        }

        public RectangleF GetBounds()
        {
            return new RectangleF(
                (float)(shape.Left / EmuPerPixel),
                (float)(shape.Top / EmuPerPixel),
                (float)(shape.Width / EmuPerPixel),
                (float)(shape.Height / EmuPerPixel));
        }
    }
}