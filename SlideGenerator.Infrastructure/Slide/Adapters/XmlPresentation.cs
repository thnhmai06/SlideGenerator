using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using SlideGenerator.Domain.Slide.Entities;
using ISlide = SlideGenerator.Domain.Slide.Entities.ISlide;
using PresentationExtension = SlideGenerator.Domain.Slide.Rules.PresentationExtension;

namespace SlideGenerator.Infrastructure.Slide.Adapters;

public class XmlPresentation(string filePath, bool isEditable = true) : IPresentation, IDisposable
{
    public string FilePath { get; } = filePath;

    public bool IsEditable { get; } = isEditable;

    internal readonly Lazy<PresentationDocument> Document = new(() =>
    {
        var docFs = new FileStream(filePath, FileMode.Open,
            isEditable ? FileAccess.ReadWrite : FileAccess.Read, FileShare.ReadWrite);
        return PresentationDocument.Open(docFs, true);
    }, LazyThreadSafetyMode.ExecutionAndPublication);

    public IEnumerable<ISlide> EnumerateSlides()
    {
        var presentationPart = Document.Value.PresentationPart
                               ?? throw new InvalidOperationException(
                                   "Invalid presentation: missing presentation part.");
        var presentation = presentationPart.Presentation ??
                           throw new InvalidOperationException("Invalid presentation: missing root presentation.");
        var slideIdList = presentation.SlideIdList
                          ?? throw new InvalidOperationException("Invalid presentation: missing slide list.");

        foreach (var (slideId, index) in slideIdList.Elements<SlideId>().Select((s, i) => (s, i + 1)))
        {
            var relId = slideId.RelationshipId?.Value;
            if (slideId.Id is null || string.IsNullOrWhiteSpace(relId))
                continue;

            var slidePart = (SlidePart)presentationPart.GetPartById(relId);
            yield return new XmlSlide(slidePart)
            {
                Id = slideId.Id.Value,
                Parent = this,
                Index = index
            };
        }
    }

    public void Save(PresentationExtension? extension = null)
    {
        if (!Document.IsValueCreated) return;
        if (extension is not null)
            Document.Value.ChangeDocumentType(extension.ToDocumentType());
        Document.Value.Save();
    }

    public void Dispose()
    {
        if (Document.IsValueCreated)
            Document.Value.Dispose();
    }
}