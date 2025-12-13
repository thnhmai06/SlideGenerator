using TaoSlideTotNghiep.Domain.Image.Enums;
using TaoSlideTotNghiep.Domain.Slide.Components;

namespace TaoSlideTotNghiep.Application.Slide.Contracts;

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