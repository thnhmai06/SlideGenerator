using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using generator.Models.Exceptions.Presentations;
using BlipFill = DocumentFormat.OpenXml.Presentation.BlipFill;
using Picture = DocumentFormat.OpenXml.Presentation.Picture;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;
using PresentationText = DocumentFormat.OpenXml.Presentation.Text;
using DrawingText = DocumentFormat.OpenXml.Drawing.Text;

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
        internal SlideIdList GetSlideIdList()
        {
            return GetPresentationPart().Presentation.SlideIdList ?? throw new NoSlideIdListException(Filepath);
        }

        internal SlidePart GetSlidePart(string slideRId)
        {
            return (SlidePart)GetPresentationPart().GetPartById(slideRId);
        }

        internal static IEnumerable<PresentationText> GetSlidePresentationText(SlidePart slidePart)
        {
            // In Textbox/Placeholder (Slide > Text (box) > Text)
            return slidePart.Slide.Descendants<PresentationText>();
        }

        internal static IEnumerable<DrawingText> GetSlideDrawingText(SlidePart slidePart)
        {
            // In Drawing objects (Shape > TextBody > Paragraph > Run > Text)
            List<DrawingText> texts = [];
            var shapes = GetSlideShapes(slidePart);
            foreach (var shape in shapes)
            {
                if (shape.TextBody is null)
                    continue;

                foreach (var paragraph in shape.TextBody.Descendants<Paragraph>())
                {
                    foreach (var run in paragraph.Descendants<Run>())
                    {
                        if (run.Text is not null)
                        {
                            texts.Add(run.Text);
                        }
                    }
                }
            }

            return texts;
        }

        internal static IEnumerable<Shape> GetSlideShapes(SlidePart slidePart, bool mustFilledByImage = false)
        {
            var shapes = slidePart.Slide.Descendants<Shape>();
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

        internal static IEnumerable<Picture> GetSlidePictures(SlidePart slidePart)
        {
            return slidePart.Slide.Descendants<Picture>();
        }
    }
}
