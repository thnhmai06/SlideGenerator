using DocumentFormat.OpenXml.Presentation;
using generator.Models.Exceptions.Presentations;

namespace generator.Models.Classes.Presentations
{
    public sealed class TemplatePresentation : Presentation
    {
        private const int FirstSlideIndex = 0;
        private readonly string _mainSlideRelationshipId;

        public TemplatePresentation(string filepath) : base(filepath, true)
        {
            var slideIds = GetSlideIdList().ChildElements;
            if (slideIds.Count != 1)
                throw new NotOnlySlidePresentationException(filepath, slideIds.Count);

            var slideId = (SlideId)slideIds[FirstSlideIndex];
            _mainSlideRelationshipId = slideId.RelationshipId?.Value ?? throw new NoRelationshipIdSlideException(filepath, FirstSlideIndex + 1);
        }

        public IEnumerable<Text> GetSlideText()
        {
            return GetSlideText(_mainSlideRelationshipId);
        }
        public IEnumerable<Shape> GetSlideShapes(bool mustFilledByImage = false)
        {
            return GetSlideShapes(_mainSlideRelationshipId, mustFilledByImage);
        }
        public IEnumerable<Picture> GetSlidePictures()
        {
            return GetSlidePictures(_mainSlideRelationshipId);
        }
    }
}
