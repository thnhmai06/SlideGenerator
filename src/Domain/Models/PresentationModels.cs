using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Domain.Exceptions;
using Spire.Presentation;
using Spire.Presentation.Drawing;
using BlipFill = DocumentFormat.OpenXml.Presentation.BlipFill;
using Picture = DocumentFormat.OpenXml.Drawing.Picture;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;
using PresentationText = DocumentFormat.OpenXml.Presentation.Text;
using DrawingText = DocumentFormat.OpenXml.Drawing.Text;

namespace Domain.Models;

public abstract class Presentation(string filepath, bool isEditable) : Model,
    IDisposable
{
    public readonly string Filepath = filepath;
    private readonly PresentationDocument _doc = PresentationDocument.Open(filepath, isEditable);
    private bool _disposed;

    protected PresentationPart GetPresentationPart()
    {
        return _doc.PresentationPart ?? throw new NoPresentationPartException(Filepath);
    }

    public SlideIdList GetSlideIdList()
    {
        return GetPresentationPart().Presentation.SlideIdList ?? throw new NoSlideIdListException(Filepath);
    }

    public SlidePart GetSlidePart(string slideRId)
    {
        return (SlidePart)GetPresentationPart().GetPartById(slideRId);
    }

    public static IEnumerable<PresentationText> GetSlidePresentationText(SlidePart slidePart)
    {
        // In Textbox/Placeholder (Slide > Text (box) > Text)
        return slidePart.Slide.Descendants<PresentationText>();
    }

    public static IEnumerable<DrawingText> GetSlideDrawingText(SlidePart slidePart)
    {
        // In Drawing objects (Shape > TextBody > Paragraph > Run > Text)
        List<DrawingText> texts = [];
        var shapes = GetSlideShapes(slidePart);
        foreach (var shape in shapes)
        {
            if (shape.TextBody is null)
                continue;

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
                return fill?.GetFirstChild<BlipFill>() != null; // BlipFill -> filled by image
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

public sealed class DerivedPresentation : Presentation
{
    public DerivedPresentation(string destPath, string srcPath) : base(destPath, true)
    {
        File.Copy(srcPath, destPath, true);
    }

    public DerivedPresentation(string destPath, TemplatePresentation srcPresentation) : this(destPath,
        srcPresentation.Filepath)
    {
    }

    public SlidePart CopySlide(string slideRid, int destination)
    {
        var presentationPart = GetPresentationPart();
        var sourceSlide = GetSlidePart(slideRid);
        var newSlide = presentationPart.AddNewPart<SlidePart>();

        // Slide XML
        newSlide.FeedData(sourceSlide.GetStream());
        // Resource references
        foreach (var rel in sourceSlide.Parts)
        {
            var part = rel.OpenXmlPart;
            var rid = rel.RelationshipId;
            newSlide.AddPart(part, rid);
        }

        // Animations
        if (sourceSlide.Slide.Timing != null)
            newSlide.Slide.Timing = (Timing)sourceSlide.Slide.Timing.CloneNode(true);
        // Transition
        if (sourceSlide.Slide.Transition != null)
            newSlide.Slide.Transition = (Transition)sourceSlide.Slide.Transition.CloneNode(true);

        var slideIdList = GetSlideIdList();
        var maxId = slideIdList.ChildElements.Cast<SlideId>().Max(x => x.Id?.Value);
        var newSlideId = new SlideId
        {
            Id = maxId + 1 ?? 0,
            RelationshipId = presentationPart.GetIdOfPart(newSlide)
        };
        if (destination <= 0 || destination > slideIdList.Count())
            slideIdList.Append(newSlideId);
        else
            slideIdList.InsertAt(newSlideId, destination - 1);

        presentationPart.Presentation.Save();
        return newSlide;
    }
}

public sealed class TemplatePresentation : Presentation
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

    public Dictionary<uint, (string, Stream)> GetAllImageShape()
    {
        Dictionary<uint, (string, Stream)> images = [];
        foreach (var shape in _spireMainSlide.Shapes.ToArray())
        {
            if (shape.IsHidden) continue;
            if (shape is SlidePicture || shape.Fill.FillType == FillFormatType.Picture)
                images.Add(shape.Id, (shape.Name, shape.SaveAsImage()));
        }

        return images;
    }
}