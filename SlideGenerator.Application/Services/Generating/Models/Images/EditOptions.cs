using SlideGenerator.Domain.Images.Models;

namespace SlideGenerator.Application.Services.Generating.Models.Images;

/// <summary>
///     Defines image edit configuration for one image instruction.
/// </summary>
/// <param name="RoiOption">ROI strategy with its type-specific options.</param>
public sealed record EditOptions(RoiOption RoiOption);
