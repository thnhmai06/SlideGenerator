namespace SlideGenerator.Images.Models;

/// <summary>Identifies the algorithm used to calculate the Region of Interest (ROI).</summary>
public enum RoiType
{
    /// <summary>Aligns the ROI based on the image center or detected faces.</summary>
    Center,

    /// <summary>Aligns the ROI based on the Rule of Thirds grid points.</summary>
    RuleOfThirds
}