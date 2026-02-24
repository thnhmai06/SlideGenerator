using SlideGenerator.Framework.Image.Models.Roi;

namespace SlideGenerator.Generating.Models;

/// <summary>
///     Represents an image binding configuration for replacement.
/// </summary>
/// <param name="ShapeId">Target shape identifier in template slide.</param>
/// <param name="Columns">Candidate columns used to resolve image source value.</param>
/// <param name="RoiType">ROI mode used for image crop and placement.</param>
public sealed record ImageConfig(uint ShapeId, IReadOnlyList<string> Columns, RoiType RoiType);