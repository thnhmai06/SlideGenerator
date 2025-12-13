using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Spire.Presentation;
using Spire.Presentation.Drawing;
using TaoSlideTotNghiep.Domain.Slide.Components;
using TaoSlideTotNghiep.Domain.Slide.Interfaces;
using TaoSlideTotNghiep.Infrastructure.Exceptions.Slide;

namespace TaoSlideTotNghiep.Infrastructure.Engines.Slide.Models;

public sealed class TemplatePresentation : Presentation, ITemplatePresentation
{
    private const int FirstSlideIndex = 0;
    private readonly Spire.Presentation.Presentation _spirePresentation = new();
    private readonly ISlide _spireMainSlide;
    private readonly string _mainSlideRid;

    public TemplatePresentation(string filepath) : base(filepath, true)
    {
        var slideIds = GetSlideIdList().ChildElements;
        if (slideIds.Count != 1)
            throw new NotOnlySlidePresentationException(filepath, slideIds.Count);

        var slideId = (SlideId)slideIds[FirstSlideIndex];
        _mainSlideRid = slideId.RelationshipId?.Value ??
                        throw new NoRelationshipIdSlideException(filepath, FirstSlideIndex + 1);
        _spirePresentation.LoadFromFile(filepath);
        _spireMainSlide = _spirePresentation.Slides[FirstSlideIndex];
    }

    public SlidePart GetSlidePart()
    {
        return GetSlidePart(_mainSlideRid);
    }

    public Dictionary<uint, ShapeImageData> GetAllImageShapes()
    {
        Dictionary<uint, ShapeImageData> shapes = [];
        foreach (var shape in _spireMainSlide.Shapes.ToArray())
        {
            if (shape.IsHidden) continue;
            if (shape is SlidePicture || shape.Fill.FillType == FillFormatType.Picture)
            {
                using var imageStream = shape.SaveAsImage();
                using var ms = new MemoryStream();
                imageStream.CopyTo(ms);
                shapes.Add(shape.Id, new ShapeImageData(shape.Name, ms.ToArray()));
            }
        }

        return shapes;
    }
}