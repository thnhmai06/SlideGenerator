using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using TaoSlideTotNghiep.Infrastructure.Exceptions.Slide;
using BlipFill = DocumentFormat.OpenXml.Presentation.BlipFill;
using Picture = DocumentFormat.OpenXml.Drawing.Picture;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;
using Text = DocumentFormat.OpenXml.Presentation.Text;

namespace TaoSlideTotNghiep.Infrastructure.Engines.Slide.Models;

public abstract class Presentation(string filepath, bool isEditable) : Model, IDisposable
{
    public string FilePath { get; } = filepath;
    private readonly PresentationDocument _doc = PresentationDocument.Open(filepath, isEditable);
    private bool _disposed;

    protected PresentationPart GetPresentationPart()
    {
        return _doc.PresentationPart ?? throw new NoPresentationPartException(FilePath);
    }

    public SlideIdList GetSlideIdList()
    {
        return GetPresentationPart().Presentation.SlideIdList ?? throw new NoSlideIdListException(FilePath);
    }

    public SlidePart GetSlidePart(string slideRId)
    {
        return (SlidePart)GetPresentationPart().GetPartById(slideRId);
    }

    public static IEnumerable<Text> GetSlidePresentationText(SlidePart slidePart)
    {
        return slidePart.Slide.Descendants<Text>();
    }

    public static IEnumerable<DocumentFormat.OpenXml.Drawing.Text> GetSlideDrawingText(SlidePart slidePart)
    {
        List<DocumentFormat.OpenXml.Drawing.Text> texts = [];
        var shapes = GetSlideShapes(slidePart);
        foreach (var shape in shapes)
        {
            if (shape.TextBody is null) continue;

            foreach (var paragraph in shape.TextBody.Descendants<Paragraph>())
            foreach (var run in paragraph.Descendants<Run>())
                if (run.Text is not null)
                    texts.Add(run.Text);
        }

        return texts;
    }

    public static IEnumerable<Shape> GetSlideShapes(SlidePart slidePart, bool mustFilledByImage = false)
    {
        var shapes = slidePart.Slide.Descendants<Shape>();
        if (mustFilledByImage)
            return shapes.Where(shape =>
            {
                var fill = shape.ShapeProperties?.GetFirstChild<FillProperties>();
                return fill?.GetFirstChild<BlipFill>() != null;
            });

        return shapes;
    }

    public static Shape? GetShapeInSlide(SlidePart slidePart, uint shapeId)
    {
        return GetSlideShapes(slidePart)
            .FirstOrDefault(shape => shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Id?.Value == shapeId);
    }

    public static IEnumerable<Picture> GetSlidePictures(SlidePart slidePart)
    {
        return slidePart.Slide.Descendants<Picture>();
    }

    public static Picture? GetPictureInSlide(SlidePart slidePart, uint shapeId)
    {
        return GetSlidePictures(slidePart)
            .FirstOrDefault(pic => pic.NonVisualPictureProperties?.NonVisualDrawingProperties?.Id?.Value == shapeId);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _doc.Dispose();
        GC.SuppressFinalize(this);
    }
}