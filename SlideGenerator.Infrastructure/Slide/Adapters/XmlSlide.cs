using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using LinqKit;
using SlideGenerator.Domain.Slide.Entities;
using SlideGenerator.Domain.Slide.Interfaces;
using SlideGenerator.Domain.Slide.Models;
using Picture = DocumentFormat.OpenXml.Presentation.Picture;
using Shape = DocumentFormat.OpenXml.Presentation.Shape;

namespace SlideGenerator.Infrastructure.Slide.Adapters;

public class XmlSlide : ISlide, ICopyable<XmlSlide>
{
    internal readonly SlidePart Core;

    internal XmlSlide(SlidePart core)
    {
        Core = core;
    }
    
    public required XmlPresentation Parent { get; init; }
    public required int Index { get; init; }
    public required uint Id { get; init; }

    public IEnumerable<IImageShape> EnumerateImageShapes()
    {
        var slide = Core.Slide;
        if (slide == null) yield break;

        var pictures = slide.Descendants<Picture>();
        var shapes = slide.Descendants<Shape>()
            .Where(s => s.ShapeProperties?.GetFirstChild<BlipFill>()?.Blip?.Embed != null);

        foreach (var pic in pictures)
            yield return new XmlImageShape(pic) { Parent = this };
        foreach (var shape in shapes)
            yield return new XmlImageShape(shape) { Parent = this };
    }

    public XmlSlide Copy(int position)
    {
        --position; // convert to 0-based index for easier handling
        var presentationPart = Parent.Document.Value.PresentationPart ?? throw new InvalidOperationException(
            "Invalid presentation: missing presentation part.");

        var oldSlide = Core.Slide ??
                       throw new InvalidOperationException("Invalid slide: missing slide content.");
        var newSlide = presentationPart.AddNewPart<SlidePart>();
        newSlide.Slide = (DocumentFormat.OpenXml.Presentation.Slide)(oldSlide.CloneNode(true)
                                                                     ?? throw new InvalidOperationException(
                                                                         "Failed to clone slide content."));
        var ridMap = new Dictionary<string, string>(StringComparer.Ordinal);

        // reuse OpenXmlPart (image/layout/chart/...) but re-create relationships (new rId)
        foreach (var rel in Core.Parts)
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
        foreach (var dpr in Core.DataPartReferenceRelationships)
        {
            var oldRid = dpr.Id; // rId in slide xml
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
        foreach (var ext in Core.ExternalRelationships)
        {
            var oldRid = ext.Id;
            if (string.IsNullOrWhiteSpace(oldRid))
                continue;

            var created = newSlide.AddExternalRelationship(ext.RelationshipType, ext.Uri);
            ridMap[oldRid] = created.Id;
        }

        // hyperlink relationships: re-create and remap
        foreach (var link in Core.HyperlinkRelationships)
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
        if (position <= 0 || position > slideCount)
        {
            slideIdList.Append(newSlideId);
            index = slideCount;
        }
        else
        {
            slideIdList.InsertAt(newSlideId, position);
            index = position + 1;
        }
        Parent.Save();
        
        return new XmlSlide(newSlide)
        {
            Parent = Parent,
            Id = nextId,
            Index = index
        };
    }

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
                    attrs[i] = new OpenXmlAttribute(a.Prefix, a.LocalName, a.NamespaceUri, newRid);
                    changed = true;
                }
            }

            if (changed)
                el.SetAttributes(attrs);
        }
    }

    private static void RemoveElementsReferencingRelIds(OpenXmlElement root, HashSet<string> relIds)
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