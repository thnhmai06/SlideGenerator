namespace SlideGenerator.Domain.Image.Rules;

/// <summary>
///     Specifies the region of interest (ROI) detection type for image cropping.
/// </summary>
public enum RoiType
{
    /// <summary>
    ///     Uses face detection (eye landmarks) to anchor a rule-of-thirds crop.
    /// </summary>
    RuleOfThirds,

    /// <summary>
    ///     Uses the center region of the image.
    /// </summary>
    Center
}