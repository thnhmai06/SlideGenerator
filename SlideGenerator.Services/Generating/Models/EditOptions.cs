using SlideGenerator.Images.Models.Options;

namespace SlideGenerator.Services.Generating.Models;

/// <summary>
///     Defines the processing rules for image transformations.
/// </summary>
/// <param name="RoiOption">The algorithm to use for Region of Interest (ROI) detection and cropping.</param>
public sealed record EditOptions(RoiOption RoiOption);