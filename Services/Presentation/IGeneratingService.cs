using TaoSlideTotNghiep.Models.Presentations;

namespace TaoSlideTotNghiep.Services.Presentation;

public interface IGeneratingService : IPresentationService
{
    DerivedPresentation GetDerivedPresentation(string filepath);
}
