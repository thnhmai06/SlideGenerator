using SlideGenerator.Domain.Image.Enums;
using SlideGenerator.Domain.Slide.Components;

namespace SlideGenerator.Application.Slide.Contracts;

public interface ISlideServices
{
    Task ProcessRowAsync(string outputPath,
        string templatePath,
        TextConfig[] textConfigs,
        ImageConfig[] imageConfigs,
        Dictionary<string, string?> rowData,
        ImageRoiType roiType,
        CancellationToken cancellationToken);
}