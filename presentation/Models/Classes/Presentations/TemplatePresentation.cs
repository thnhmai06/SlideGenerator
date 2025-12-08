using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using presentation.Models.Exceptions.Presentations;
using Spire.Presentation;
using Spire.Presentation.Drawing;
using SpirePresentation = Spire.Presentation.Presentation;
using ISpireSlide = Spire.Presentation.ISlide;

namespace presentation.Models.Classes.Presentations
{
    public sealed class TemplatePresentation : Presentation
    {
        private const int FirstSlideIndex = 0;
        private readonly SpirePresentation _spirePresentation = new();
        private readonly ISpireSlide _spireMainSlide;
        private readonly string _mainSlideRid;

        public TemplatePresentation(string filepath) : base(filepath, true)
        {
            var slideIds = GetSlideIdList().ChildElements;
            if (slideIds.Count != 1)
                throw new NotOnlySlidePresentationException(filepath, slideIds.Count);

            var slideId = (SlideId)slideIds[FirstSlideIndex];
            _mainSlideRid = slideId.RelationshipId?.Value ?? throw new NoRelationshipIdSlideException(filepath, FirstSlideIndex + 1);
            _spirePresentation.LoadFromFile(filepath);
            _spireMainSlide = _spirePresentation.Slides[FirstSlideIndex];
        }

        internal SlidePart GetSlidePart()
        {
            return GetSlidePart(_mainSlideRid);
        }

        internal Dictionary<uint, (string, Stream)> GetAllImageShape()
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
}
