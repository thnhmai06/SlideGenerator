using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace TaoSlideTotNghiep.Domain.Interfaces;

public interface IDerivedPresentation
{
    SlidePart CopySlide(string slideRid, int destination);
    SlideIdList GetSlideIdList();
    SlidePart GetSlidePart(string slideRId);
}

public interface ITemplatePresentation
{
    SlidePart GetSlidePart();
    SlidePart GetSlidePart(string slideRId);
    Dictionary<uint, (string, Stream)> GetAllImageShape();
    SlideIdList GetSlideIdList();
}