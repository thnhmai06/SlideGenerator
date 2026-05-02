using Microsoft.Extensions.DependencyInjection;
using Syncfusion.XlsIO;

namespace SlideGenerator.Sheets;

public static class Registration
{
    public static IServiceCollection AddSheetServices(this IServiceCollection services)
    {
        services.AddSingleton<ExcelEngine>();
        return services;
    }
}