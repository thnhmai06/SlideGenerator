using TaoSlideTotNghiep.Domain.Slide.Components;

namespace TaoSlideTotNghiep.Domain.Slide.Interfaces;

public interface ITemplatePresentation : IDisposable
{
    string FilePath { get; }
    Dictionary<uint, ShapeImageData> GetAllImageShapes();
}