using Microsoft.Extensions.DependencyInjection;
using OpenCvSharp;
using SlideGenerator.Images.Entities.Detectors;
using SlideGenerator.Images.Services;

namespace SlideGenerator.Images;

public static class Registration
{
    private const string ModelPath = @"Binary\YuNet.onnx";
    private const float Confidence = 0.8f;
    private static readonly Size InputSize = new(640, 640);

    public static IServiceCollection AddImageServices(this IServiceCollection services)
    {
        services.AddSingleton<FaceDetector>(_ =>
            new YuNet(
                FaceDetectorYN.Create(ModelPath, string.Empty, InputSize, Confidence),
                InputSize));
        services.AddSingleton<RoiResolver>();
        return services;
    }
}