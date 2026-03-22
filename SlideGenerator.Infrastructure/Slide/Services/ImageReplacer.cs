using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using Picture = DocumentFormat.OpenXml.Presentation.Picture;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;

namespace SlideGenerator.Infrastructure.Slide.Services;

/// <summary>
///     Provides image replacement functionality for slides.
/// </summary>
/// Reviewed by @thnhmai06 at 05/03/2026
public static class ImageReplacer
{
    /// <param name="slidePart">The slide part containing the picture.</param>
    extension(SlidePart slidePart)
    {
        /// <summary>
        ///     Replaces the image in a <see cref="Picture" /> element.
        /// </summary>
        /// <param name="picture">The picture element to update.</param>
        /// <param name="image">Stream containing the new image data (PNG format).</param>
        /// <returns>
        ///     <see langword="false" /> if provided <see cref="Picture" /> does not contain image; otherwise,
        ///     <see langword="true" />.
        /// </returns>
        public bool ReplaceImage(Picture picture, Stream image)
        {
            var blip = picture.Descendants<Blip>().FirstOrDefault();
            var embed = blip?.Embed;
            if (embed == null) return false;

            var imgPart = slidePart.AddImagePart(ImagePartType.Png);
            imgPart.FeedData(image);
            var rId = slidePart.GetIdOfPart(imgPart);

            embed.Value = rId;
            return true;
        }

        /// <summary>
        ///     Replaces the image in a <see cref="Shape" /> element (which filled with image).
        /// </summary>
        /// <param name="shape">The shape element to update.</param>
        /// <param name="image">Stream containing the new image data (PNG format).</param>
        /// <returns><see langword="false" /> if provided shape is not filled by image; otherwise, <see langword="true" />.</returns>
        public bool ReplaceImage(Shape shape, Stream image)
        {
            var blipFill = shape.ShapeProperties?.GetFirstChild<BlipFill>();
            var embed = blipFill?.Blip?.Embed;
            if (embed == null) return false;

            var imgPart = slidePart.AddImagePart(ImagePartType.Png);
            imgPart.FeedData(image);
            var rId = slidePart.GetIdOfPart(imgPart);

            embed.Value = rId;
            return true;
        }
    }
}