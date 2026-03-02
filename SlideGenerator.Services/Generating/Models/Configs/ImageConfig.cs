using SlideGenerator.Framework.Features.Image.Models.Roi;
using SlideGenerator.Services.Generating.Models.Info;

namespace SlideGenerator.Services.Generating.Models.Configs;

/// <summary>
///     Represents an image binding configuration for replacement.
/// </summary>
/// <param name="Shape">The shape wants to replace.</param>
/// <param name="Columns">Candidate columns used to resolve image source value.</param>
/// <param name="RoiType">ROI mode used for image crop and placement.</param>
public sealed record ImageConfig(ShapeInfo Shape, IReadOnlyList<string> Columns, RoiType RoiType);