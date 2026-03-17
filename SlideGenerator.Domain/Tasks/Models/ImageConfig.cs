using SlideGenerator.Framework.Image.Models.Roi;

namespace SlideGenerator.Domain.Tasks.Models;

/// <summary>
///     Represents an image binding configuration for replacement.
/// </summary>
/// <param name="Shape">The shape wants to replace.</param>
/// <param name="Columns">Candidate columns used to resolve image source value.</param>
/// <param name="RoiType">ROI mode used for image crop and placement.</param>
public sealed record ImageConfig(ShapeInfo Shape, IReadOnlyList<string> Columns, RoiType RoiType);

internal static class ImageConfigExtensions
{
    /// <summary>
    ///     Converts a <see cref="ImageConfig" /> to many <see cref="ImageFlatConfig" />s
    ///     by flattening shape and columns information.
    /// </summary>
    internal static IEnumerable<ImageFlatConfig> Flatten(this ImageConfig config)
    {
        var shapeId = config.Shape.Id;
        var roiType = config.RoiType;
        return config.Columns.Select(col => new ImageFlatConfig(shapeId, col, roiType));
    }
}