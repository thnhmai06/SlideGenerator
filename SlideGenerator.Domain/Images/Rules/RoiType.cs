namespace SlideGenerator.Domain.Images.Rules;

/// <summary>
///     Specifies the region of interest (ROI) detection type for image cropping.
/// </summary>
public enum RoiType
{
    /// <summary>
    ///     Crops by keeping the visual center of the image.
    /// </summary>
    Center,

    /// <summary>
    ///     Crops by placing detected eye-line around the upper third area.
    /// </summary>
    RuleOfThirds
}