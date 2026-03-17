using SlideGenerator.Framework.Image.Models.Roi;

namespace SlideGenerator.Domain.Tasks.Models;

/// <summary>
///     Flat version of <see cref="ImageConfig" /> with only necessary information for image processing and downloading.
/// </summary>
internal record ImageFlatConfig(uint ShapeId, string Column, RoiType RoiType);