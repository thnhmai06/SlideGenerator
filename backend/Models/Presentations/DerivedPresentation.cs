using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace TaoSlideTotNghiep.Models.Presentations;

public sealed class DerivedPresentation : Presentation
{
    public DerivedPresentation(string destPath, string srcPath) : base(destPath, true)
    {
        File.Copy(srcPath, destPath, true);
    }

    public DerivedPresentation(string destPath, TemplatePresentation srcPresentation) : this(destPath, srcPresentation.Filepath) { }

    internal SlidePart CopySlide(string slideRid, int destination)
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
