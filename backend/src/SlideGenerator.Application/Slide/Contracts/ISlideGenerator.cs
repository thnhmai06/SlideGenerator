using SlideGenerator.Domain.Image.Enums;
using SlideGenerator.Domain.Slide.Components;

namespace SlideGenerator.Application.Slide.Contracts;

public interface ISlideGenerator
{
    Task ProcessRowAsync(
        string derivedPresentationPath,
        string templatePath,
        Dictionary<string, string?> rowData,
        TextConfig[] textConfigs,
        ImageConfig[] imageConfigs,
        ImageRoiType roiType,
        CancellationToken cancellationToken);
}