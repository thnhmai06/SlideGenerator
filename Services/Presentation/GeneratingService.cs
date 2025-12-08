using TaoSlideTotNghiep.Models.Presentations;
using TaoSlideTotNghiep.Exceptions.Services;

namespace TaoSlideTotNghiep.Services.Presentation;

public sealed class GeneratingService : PresentationService, IGeneratingService
{
    protected override Models.Presentations.Presentation OpenPresentation(string filepath, string? sourcePath)
    {
        return sourcePath is null 
            ? throw new NotEnoughArgumentException(filepath) 
            : new DerivedPresentation(filepath, sourcePath);
    }

    public DerivedPresentation GetDerivedPresentation(string filepath)
    {
        return (DerivedPresentation)GetPresentation(filepath);
    }

    //TODO: Write Generating Services
}
