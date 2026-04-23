using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using LinqKit;
using SlideGenerator.Domain.Slides.Entities.Presentation;
using SlideGenerator.Domain.Slides.Entities.Slide;
using SlideGenerator.Domain.Slides.Models.Identifiers;
using PresentationExtension = SlideGenerator.Domain.Slides.Rules.PresentationExtension;

namespace SlideGenerator.Infrastructure.Slides.Adapters;

/// <summary>
///     Represents a PowerPoint presentation implementation based on Open XML SDK.
/// </summary>
/// <param name="filePath">The absolute path to the presentation file.</param>
/// <param name="isEditable">Indicates whether the presentation is opened in read-write mode.</param>
public class XmlPresentation(string filePath, bool isEditable = true) : IPresentation, IDisposable
{
    /// <summary>
    ///     Lazy initializer for the underlying <see cref="PresentationDocument" />.
    /// </summary>
    private readonly Lazy<PresentationDocument> _core = new(() =>
    {
        var docFs = new FileStream(filePath, FileMode.Open,
            isEditable ? FileAccess.ReadWrite : FileAccess.Read, FileShare.ReadWrite);
        return PresentationDocument.Open(docFs, isEditable);
    }, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    ///     Releases the resources used by the <see cref="XmlPresentation" />.
    /// </summary>
    public void Dispose()
    {
        if (_core.IsValueCreated)
            _core.Value.Dispose();
    }

    /// <summary>
    ///     Gets the unique identifier for this presentation.
    /// </summary>
    public PresentationIdentifier Identifier { get; } = new(filePath);

    /// <summary>
    ///     Enumerates all slides in the presentation.
    /// </summary>
    /// <returns>An enumerable collection of <see cref="ISlide" /> objects.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the presentation structure is invalid.</exception>
    public IEnumerable<ISlide> EnumerateSlides()
    {
        var presentationPart = _core.Value.PresentationPart
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
            yield return new XmlSlide(this, slidePart)
            {
                Id = slideId.Id.Value,
                Index = index
            };
        }
    }

    /// <summary>
    ///     Copies a slide from one position to another.
    /// </summary>
    /// <param name="from">The 1-based index of the source slide.</param>
    /// <param name="to">The 1-based index where the slide should be inserted.</param>
    /// <returns>The newly created <see cref="ISlide" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="from" /> is less than or equal to zero.</exception>
    /// <exception cref="ArgumentException">Thrown when the source slide position is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the presentation or slide part is missing or corrupted.</exception>
    public ISlide CopySlide(int from, int to)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(from);

        var sourceSlide = EnumerateSlides().ElementAtOrDefault(from - 1) as XmlSlide
                          ?? throw new ArgumentException("Invalid source slide position.", nameof(from));

        var insertAt = to - 1; // convert to 0-based index for easier handling
        var presentationPart = _core.Value.PresentationPart ?? throw new InvalidOperationException(
            "Invalid presentation: missing presentation part.");

        var oldSlide = sourceSlide.Core.Slide ??
                       throw new InvalidOperationException("Invalid slide: missing slide content.");
        var newSlide = presentationPart.AddNewPart<SlidePart>();
        newSlide.Slide = (Slide)(oldSlide.CloneNode(true)
                                 ?? throw new InvalidOperationException(
                                     "Failed to clone slide content."));
        var ridMap = new Dictionary<string, string>(StringComparer.Ordinal);

        // reuse OpenXmlPart (image/layout/chart/...) but re-create relationships (new rId)
        foreach (var rel in sourceSlide.Core.Parts)
        {
            // avoid copying NotesSlidePart: sharing notes across slides often corrupts package
            if (rel.OpenXmlPart is NotesSlidePart)
                continue;

            var oldRid = rel.RelationshipId;
            if (string.IsNullOrWhiteSpace(oldRid))
                continue;

            var added = newSlide.AddPart(rel.OpenXmlPart);
            var newRid = newSlide.GetIdOfPart(added);

            ridMap[oldRid] = newRid;
        }

        // reuse media (video/audio/media) by re-creating relationships and remap rId
        var unsupportedDataRelIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dpr in sourceSlide.Core.DataPartReferenceRelationships)
        {
            var oldRid = dpr.Id; // rId in slide XML
            if (string.IsNullOrWhiteSpace(oldRid))
                continue;

            if (dpr.DataPart is MediaDataPart media)
            {
                // Create a new relationship (new rId), then remap slide XML to that id
                var newRid = dpr switch
                {
                    VideoReferenceRelationship => newSlide.AddVideoReferenceRelationship(media).Id,
                    AudioReferenceRelationship => newSlide.AddAudioReferenceRelationship(media).Id,
                    _ => newSlide.AddMediaReferenceRelationship(media).Id
                };

                ridMap[oldRid] = newRid;
            }
            else
            {
                // Other DataPart kinds (OLE/controls/embedded) are not safely addable via public SDK API
                unsupportedDataRelIds.Add(oldRid);
            }
        }

        // external relationships: re-create and remap
        foreach (var ext in sourceSlide.Core.ExternalRelationships)
        {
            var oldRid = ext.Id;
            if (string.IsNullOrWhiteSpace(oldRid))
                continue;

            var created = newSlide.AddExternalRelationship(ext.RelationshipType, ext.Uri);
            ridMap[oldRid] = created.Id;
        }

        // hyperlink relationships: re-create and remap
        foreach (var link in sourceSlide.Core.HyperlinkRelationships)
        {
            var oldRid = link.Id;
            if (string.IsNullOrWhiteSpace(oldRid))
                continue;

            var created = newSlide.AddHyperlinkRelationship(link.Uri, link.IsExternal);
            ridMap[oldRid] = created.Id;
        }

        // strip unsupported DataPartReferenceRelationship to avoid dangling rIds -> PowerPoint repair/corrupt
        if (unsupportedDataRelIds.Count > 0)
            RemoveElementsReferencingRelIds(newSlide.Slide, unsupportedDataRelIds);

        // rewrite r:id / r:embed / r:link in slide XML to the newly created relationship ids
        RemapRelIds(newSlide.Slide, ridMap);

        newSlide.Slide.Save();

        // add slide to SlideIdList
        var slideIdList = presentationPart.Presentation!.SlideIdList!;
        uint nextId = 256;
        var hasIds = false;
        uint maxId = 0;
        foreach (var slideId in slideIdList.ChildElements.OfType<SlideId>())
        {
            var idValue = slideId.Id?.Value;
            if (!idValue.HasValue) continue;
            if (!hasIds || idValue.Value > maxId)
            {
                maxId = idValue.Value;
                hasIds = true;
            }
        }

        if (hasIds) nextId = maxId + 1;
        var newSlideId = new SlideId
        {
            Id = nextId,
            RelationshipId = presentationPart.GetIdOfPart(newSlide)
        };
        var slideCount = slideIdList.Count();
        int index;
        if (insertAt <= 0 || insertAt > slideCount)
        {
            slideIdList.Append(newSlideId);
            index = slideCount;
        }
        else
        {
            slideIdList.InsertAt(newSlideId, insertAt);
            index = insertAt + 1;
        }

        Save();

        return new XmlSlide(this, newSlide)
        {
            Id = nextId,
            Index = index
        };
    }

    /// <summary>
    ///     Removes a slide from the presentation.
    /// </summary>
    /// <param name="index">The 1-based index of the slide to remove.</param>
    /// <returns><see langword="true" /> if the slide was successfully removed; otherwise, <see langword="false" />.</returns>
    public bool RemoveSlide(int index)
    {
        if (index <= 0)
            return false;

        var slideIdList = _core.Value.PresentationPart?.Presentation?.SlideIdList;

        var target = slideIdList?.Elements<SlideId>().ElementAtOrDefault(index - 1);
        if (target == null)
            return false;

        target.Remove();
        Save();
        return true;
    }

    /// <summary>
    ///     Saves the presentation changes.
    /// </summary>
    /// <param name="extension">Optional document extension to change the presentation type.</param>
    public void Save(PresentationExtension? extension = null)
    {
        if (!_core.IsValueCreated) return;
        if (extension is not null)
            _core.Value.ChangeDocumentType(extension.ToXmlDocType());
        _core.Value.Save();
    }

    /// <summary>
    ///     Remaps relationship identifiers in the XML content.
    /// </summary>
    /// <param name="root">The root <see cref="OpenXmlElement" /> to process.</param>
    /// <param name="ridMap">A dictionary mapping old relationship IDs to new ones.</param>
    private static void RemapRelIds(OpenXmlElement root, Dictionary<string, string> ridMap)
    {
        const string relNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        foreach (var el in root.Descendants())
        {
            var attrs = el.GetAttributes();
            if (attrs.Count == 0) continue;

            var changed = false;

            for (var i = 0; i < attrs.Count; i++)
            {
                var a = attrs[i];
                if (a.NamespaceUri != relNs) continue;
                if (a.LocalName is not ("id" or "embed" or "link")) continue;
                if (string.IsNullOrEmpty(a.Value)) continue;

                if (ridMap.TryGetValue(a.Value, out var newRid) &&
                    !string.Equals(a.Value, newRid, StringComparison.Ordinal))
                {
                    attrs[i] = new OpenXmlAttribute(a.Prefix, a.LocalName, a.NamespaceUri,
                        newRid);
                    changed = true;
                }
            }

            if (changed)
                el.SetAttributes(attrs);
        }
    }

    /// <summary>
    ///     Removes elements that reference specific relationship identifiers.
    /// </summary>
    /// <param name="root">The root <see cref="OpenXmlElement" /> to process.</param>
    /// <param name="relIds">A set of relationship IDs to remove.</param>
    private static void RemoveElementsReferencingRelIds(OpenXmlElement root,
        HashSet<string> relIds)
    {
        // r:id, r:embed, r:link are in namespace relationships
        const string relNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        var toRemove = root
            .Descendants()
            .Where(el => el.GetAttributes().Any(a =>
                a is { NamespaceUri: relNs, LocalName: "id" or "embed" or "link", Value: not null } &&
                relIds.Contains(a.Value)));
        toRemove.ForEach(el => el.Remove());
    }
}