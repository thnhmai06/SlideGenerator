using SlideGenerator.Framework.Image.Models.Roi;

namespace SlideGenerator.Domain.Tasks.Models;

/// <summary>
///     Represents an image binding configuration for replacement.
/// </summary>
/// <param name="Shape">The shape wants to replace.</param>
/// <param name="Columns">Candidate columns used to resolve image source value.</param>
/// <param name="RoiType">ROI mode used for image crop and placement.</param>
public sealed record ImageConfig(ShapeInfo Shape, IReadOnlyList<string> Columns, RoiType RoiType);