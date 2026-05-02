namespace SlideGenerator.Images.Models.Options;

/// <summary>
///     Customizes the center ROI calculation.
/// </summary>
public sealed class CenterOption : RoiOption
{
    /// <summary>
    ///     Gets or sets whether to use detected faces to align the center.
    /// </summary>
    public bool UseFaceAlignment { get; init; }

    public override RoiType Type => RoiType.Center;
}