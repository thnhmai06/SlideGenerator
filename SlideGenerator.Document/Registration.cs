using Microsoft.Extensions.DependencyInjection;
using SlideGenerator.Document.Slide.Services;
using SlideGenerator.Documents;
using Syncfusion.Licensing;
using Syncfusion.XlsIO;
using TextComposer = SlideGenerator.Document.Slide.Services.TextComposer;

namespace SlideGenerator.Document;

/// <summary>
///     Provides extension methods to register document-related services.
/// </summary>
public static class Registration
{
    public static IServiceCollection AddDocumentServices(this IServiceCollection services)
    {
        var licenseKey = SyncfusionLicense.Key; // decoded from XOR-encoded bytes at build time
        if (!string.IsNullOrWhiteSpace(licenseKey) && licenseKey != "empty")
            SyncfusionLicenseProvider.RegisterLicense(licenseKey);

        services.AddSingleton<ExcelEngine>();
        services.AddSingleton<ImageComposer>();
        services.AddSingleton<TextComposer>();

        return services;
    }
}