using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Slides.Services;

namespace SlideGenerator.Slides;

public static class Registration
{
    public static IServiceCollection AddSlidesServices(this IServiceCollection services)
    {
        services.AddSingleton<ImageComposer>();
        services.AddSingleton<TextComposer>();
        return services;
    }
}