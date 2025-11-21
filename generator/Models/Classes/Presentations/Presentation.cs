using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using generator.Models.Exceptions.Presentations;
using BlipFill = DocumentFormat.OpenXml.Presentation.BlipFill;
using Picture = DocumentFormat.OpenXml.Presentation.Picture;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;
using Text = DocumentFormat.OpenXml.Presentation.Text;

namespace generator.Models.Classes.Presentations
{
    public abstract class Presentation(string filepath, bool isEditable)
    {
        public readonly string Filepath = filepath;
        private readonly PresentationDocument _doc = PresentationDocument.Open(filepath, isEditable);

        protected PresentationPart GetPresentationPart()
        {
            return _doc.PresentationPart ?? throw new NoPresentationPartException(Filepath);
        }
        protected SlideIdList GetSlideIdList()
        {
            return GetPresentationPart().Presentation.SlideIdList ?? throw new NoSlideIdListException(Filepath);
        }

        protected SlidePart GetSlide(string relationshipId)
        {
            return (SlidePart)GetPresentationPart().GetPartById(relationshipId);
        }
        protected IEnumerable<Text> GetSlideText(string relationshipId)
        {
            return GetSlide(relationshipId).Slide.Descendants<Text>();
        }
        protected IEnumerable<Shape> GetSlideShapes(string relationshipId, bool mustFilledByImage = false)
        {
            var shapes = GetSlide(relationshipId).Slide.Descendants<Shape>();
            if (mustFilledByImage)
            {
                return shapes.Where(shape =>
                {
                    var fill = shape.ShapeProperties?.GetFirstChild<FillProperties>();
                    return fill?.GetFirstChild<BlipFill>() != null; // BlipFill -> filled by image
                });
            }
            return shapes;
        }
        protected IEnumerable<Picture> GetSlidePictures(string relationshipId)
        {
            return GetSlide(relationshipId).Slide.Descendants<Picture>();
        }
    }
}
