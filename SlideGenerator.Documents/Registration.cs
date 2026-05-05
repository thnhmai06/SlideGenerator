using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Documents.Slides.Services;
using Syncfusion.XlsIO;
using TextComposer = SlideGenerator.Documents.Slides.Services.TextComposer;

namespace SlideGenerator.Documents;

/// <summary>
///     Provides extension methods to register document-related services.
/// </summary>
public static class Registration
{
    public static IServiceCollection AddDocumentServices(this IServiceCollection services)
    {
        var licenseKey = SyncfusionLicense.Key; // decoded from XOR-encoded bytes at build time
        if (!string.IsNullOrWhiteSpace(licenseKey) && licenseKey != "empty")
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(licenseKey);

        services.AddSingleton<ExcelEngine>();
        services.AddSingleton<ImageComposer>();
        services.AddSingleton<TextComposer>();

        return services;
    }
}
