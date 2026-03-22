namespace SlideGenerator.Domain.Tasks.Models.Image.Edits;

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
    ///     Detects the most prominent/salient region of the image using spectral residual saliency.
    /// </summary>
    Prominent,

    /// <summary>
    ///     Uses the center region of the image.
    /// </summary>
    Center
}