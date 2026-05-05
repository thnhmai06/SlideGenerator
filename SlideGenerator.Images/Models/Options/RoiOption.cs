namespace SlideGenerator.Images.Models.Options;

/// <summary>
///     Provides customization options for ROI calculation.
/// </summary>
public abstract record RoiOption
{
    /// <summary>
    ///     Gets the region of interest (ROI) detection type.
    /// </summary>
    public abstract RoiType Type { get; }
}
