using Syncfusion.Presentation;

namespace SlideGenerator.Slides.Services;

/// <summary>
///     Replaces image content in Syncfusion shapes.
/// </summary>
public static class SfImageComposer
{
    /// <summary>
    ///     Replaces the image content of a shape with the provided stream.
    /// </summary>
    /// <param name="shape">The Syncfusion shape to modify.</param>
    /// <param name="imageBytes">The stream containing the new image data.</param>
    /// <returns>The number of images replaced (usually 1 or 0).</returns>
    public static int Replace(IShape shape, byte[] imageBytes)
    {
        // Picture
        if (shape is IPicture picture)
        {
            picture.ImageData = imageBytes;
            return 1;
        }

        // BlipFill
        if (shape.Fill.FillType == FillType.Picture)
        {
            shape.Fill.PictureFill.ImageBytes = imageBytes;
            return 1;
        }

        return 0;
    }
}