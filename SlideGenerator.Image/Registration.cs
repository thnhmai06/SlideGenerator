using Microsoft.Extensions.DependencyInjection;
using OpenCvSharp;
using SlideGenerator.Image.Entities.Detectors;
using SlideGenerator.Image.Services;

namespace SlideGenerator.Image;

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