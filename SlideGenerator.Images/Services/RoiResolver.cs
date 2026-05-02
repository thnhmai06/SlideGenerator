using System.Collections.ObjectModel;
using System.Drawing;
using SlideGenerator.Images.Entities.Detectors;
using SlideGenerator.Images.Entities.ROI;
using SlideGenerator.Images.Models;
using SlideGenerator.Images.Models.Options;
using Mat = OpenCvSharp.Mat;

namespace SlideGenerator.Images.Services;

public sealed class RoiResolver(FaceDetector faceDetector)
{
    private readonly ReadOnlyDictionary<RoiType, RoiCalculator> _calculators =
        new Dictionary<RoiType, RoiCalculator>
        {
            { RoiType.Center, new CenterRoi(faceDetector) },
            { RoiType.RuleOfThirds, new RuleOfThirdsRoi(faceDetector) }
        }.AsReadOnly();

    /// <summary>
    ///     Calculates ROI by routing to the calculator keyed by the option type.
    /// </summary>
    public ValueTask<Rectangle> CalculateRoiAsync(Mat mat, Size targetSize, RoiType type, RoiOption? option = null)
    {
        return GetCalculator(type).CalculateRoiAsync(mat, targetSize, type, option);
    }

    private RoiCalculator GetCalculator(RoiType key)
    {
        return _calculators.TryGetValue(key, out var calculator) 
            ? calculator 
            : throw new ArgumentOutOfRangeException(nameof(key), key, null);
    }
}