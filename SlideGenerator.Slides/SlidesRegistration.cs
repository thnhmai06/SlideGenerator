using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Slides.Services;

namespace SlideGenerator.Slides;

public static class SlidesRegistration
{
    public static IServiceCollection AddSlideServices(this IServiceCollection services)
    {
        services.AddSingleton<SfPresentationRegistry>();
        services.AddSingleton<SfImageComposer>();
        services.AddSingleton<SfTextComposer>();
        return services;
    }
}