using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using generator.Models.Exceptions.Presentations;

namespace generator.Models.Classes.Presentations
{
    public sealed class TemplatePresentation : Presentation
    {
        private const int FirstSlideIndex = 0;
        private readonly string _mainSlideRid;

        public TemplatePresentation(string filepath) : base(filepath, true)
        {
            var slideIds = GetSlideIdList().ChildElements;
            if (slideIds.Count != 1)
                throw new NotOnlySlidePresentationException(filepath, slideIds.Count);

            var slideId = (SlideId)slideIds[FirstSlideIndex];
            _mainSlideRid = slideId.RelationshipId?.Value ?? throw new NoRelationshipIdSlideException(filepath, FirstSlideIndex + 1);
        }

        internal SlidePart GetSlidePart()
        {
            return GetSlidePart(_mainSlideRid);
        }
    }
}
