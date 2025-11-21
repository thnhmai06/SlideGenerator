using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace generator.Models.Classes.Presentations;

public sealed class DerivedPresentation : Presentation
{
    public DerivedPresentation(string destPath, string srcPath) : base(destPath, true)
    {
        File.Copy(srcPath, destPath, true);
    }

    public DerivedPresentation(string destPath, TemplatePresentation srcPresentation) : this(destPath, srcPresentation.Filepath) { }

    public SlidePart CopySlide(string relationshipId, int destination)
    {
        var presentationPart = GetPresentationPart();
        var sourceSlide = GetSlide(relationshipId);
        var newSlide = presentationPart.AddNewPart<SlidePart>();

        // Slide XML
        newSlide.FeedData(sourceSlide.GetStream());
        // Layout
        if (sourceSlide.SlideLayoutPart != null)
            newSlide.AddPart(sourceSlide.SlideLayoutPart);
        // Master part
        if (sourceSlide.SlideLayoutPart?.SlideMasterPart != null)
            newSlide.AddPart(sourceSlide.SlideLayoutPart.SlideMasterPart);
        // Images references
        foreach (var image in sourceSlide.ImageParts)
            newSlide.AddImagePart(image.ContentType, sourceSlide.GetIdOfPart(image));
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