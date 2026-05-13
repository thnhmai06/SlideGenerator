/*
 * Copyright (C) 2026 Thành Mai (thnhmai06)
 *
 * Solution: SlideGenerator
 * Project: SlideGenerator.Document
 * File: TextComposer.cs
 *
 * This file is part of this solution. You can find the full source code here: https://github.com/thnhmai06/SlideGenerator
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 */
using System.Text;
using System.Text.RegularExpressions;
using SlideGenerator.Document.Application.Abstractions;
using SlideGenerator.Document.Domain.Abstractions.Slide;

namespace SlideGenerator.Document.Application.Services;

/// <summary>
/// Renders template placeholders across all paragraphs of a shape while preserving
/// per-TextPart formatting through coverage ratio distribution.
/// </summary>
/// <remarks>
/// <para>Algorithm overview:</para>
/// <list type="number">
///   <item>Flatten all TextParts across all paragraphs into a single logical string,
///         tracking each TextPart's character range (PartSpan).</item>
///   <item>Use a two-pointer scan to locate Mustache tag boundaries and classify tags
///         as Renderable (variables) or Structural (sections/loops).</item>
///   <item>Build a marked template: wrap Renderable and static-text segments with unique
///         XML-like markers; inject paragraph-break sentinels; pass Structural tags through.</item>
///   <item>Render the marked template via <see cref="ITemplateEngine"/>.</item>
///   <item>Parse the rendered output to extract per-paragraph chunks and per-marker content.</item>
///   <item>Reconstruct the shape: for each chunk, create a new paragraph and distribute
///         the rendered text across TextParts proportionally (Coverage Ratio).</item>
/// </list>
/// </remarks>
public sealed partial class TextComposer(ITemplateEngine templateEngine) : ITextComposer
{
    #region Internal data structures

    /// <summary>
    /// Represents a span of text originating from a specific text part within a paragraph,
    /// providing metadata for its position and length within a flattened text structure.
    /// </summary>
    /// <remarks>
    /// A <c>PartSpan</c> describes a segment of text extracted from an <see cref="ITextPart"/>.
    /// It is used to map portions of text within a flattened representation back to their
    /// original sources during text transformations, ensuring that formatting and structure
    /// are preserved when reconstructing shapes or paragraphs.
    /// </remarks>
    private record PartSpan(ITextPart Source, int GlobalStart, int Length);

    /// <summary>
    /// Represents the positional endpoint of a paragraph within a flattened text structure,
    /// combining the paragraph's index and the cumulative character position at the paragraph's end.
    /// </summary>
    /// <remarks>
    /// Instances of this record are created during text flattening and are used to identify
    /// the boundaries of paragraphs for further processing and reconstruction tasks.
    /// </remarks>
    private record ParagraphEnd(int ParagraphIndex, int Position);

    /// <summary>
    /// Represents the different types of text segments that can be identified within
    /// a flattened template string.
    /// </summary>
    /// <remarks>
    /// This enum is used to classify contiguous portions of text within a template
    /// during parsing, allowing for differentiation between static text, renderable
    /// placeholders, and structural elements.
    /// </remarks>
    private enum SegmentKind
    {
        /// <summary>
        /// Static text segments that are neither placeholders nor structural elements.
        /// </summary>
        StaticText,

        /// <summary>
        /// Placeholders that can be replaced with dynamic content.
        /// </summary>
        RenderableTag,

        /// <summary>
        /// Tags that represent control structures, such as sections or loops.
        /// </summary>
        StructuralTag
    }

    /// <summary>
    /// Represents a contiguous segment derived from a flattened text string,
    /// capturing its start position, length, content, and classification.
    /// </summary>
    /// <remarks>
    /// FlatSegment serves as a building block for parsing and analyzing templates
    /// by breaking a logical input string into distinct parts: static text,
    /// renderable tags, or structural tags. Each segment identifies its position
    /// and size, facilitating structured reconstruction or processing of the text.
    /// </remarks>
    private record FlatSegment(int Start, int Length, string Content, SegmentKind Kind);

    /// <summary>
    /// Represents a mapping of a text part to its coverage ratio relative to a segment within a composition process.
    /// </summary>
    /// <remarks>
    /// This record is primarily used to track how much of a given text part contributes to a specific segment.
    /// The ratio value indicates the proportion of the text part that overlaps with the segment, enabling proportional
    /// distribution of content during text rendering and formatting.
    /// </remarks>
    private record CoverageEntry(ITextPart Source, double Ratio);

    #endregion

    #region Compiled regexes

    // Matches <pb_N/> paragraph-break sentinels injected by BuildMarkedTemplate.
    private static readonly Regex ParagraphBreakRegex = ParagraphBreakPattern();

    [GeneratedRegex(@"<pb_(\d+)/>", RegexOptions.Compiled)]
    private static partial Regex ParagraphBreakPattern();

    #endregion

    #region Public API

    /// <inheritdoc />
    public void Compose(IShape shape, IReadOnlyDictionary<string, string> resolvedValue)
    {
        if (shape.ParagraphsCount == 0 || resolvedValue.Count == 0) return;

        var (flat, partSpans, paraEnds) = FlattenShape(shape);
        if (string.IsNullOrWhiteSpace(flat)) return;

        var segments = ScanSegments(flat);
        var (markedTemplate, markerMap) = BuildMarkedTemplate(flat, segments, partSpans, paraEnds);
        var rendered = templateEngine.Render(markedTemplate, resolvedValue);
        var validParaIndices = paraEnds.Select(e => e.ParagraphIndex).ToHashSet();
        var chunks = ParseChunks(rendered, markerMap, validParaIndices);
        ReconstructShape(shape, chunks, markerMap);
    }

    #endregion

    #region Step 1: Flatten

    /// <summary>
    /// Flattens the hierarchical structure of the shape into a single string, while also mapping
    /// text parts and paragraph ends to their corresponding positions in the flat representation.
    /// </summary>
    /// <param name="shape">The shape containing paragraphs and text parts to be flattened.</param>
    /// <returns>
    /// A tuple containing:
    /// - A string representing the concatenated text of all paragraphs and text parts.
    /// - A read-only list mapping text parts to their positions and lengths in the flat string.
    /// - A read-only list indicating the end positions of each paragraph in the flat string.
    /// </returns>
    private static (string Flat, IReadOnlyList<PartSpan> PartSpans, IReadOnlyList<ParagraphEnd> Ends)
        FlattenShape(IShape shape)
    {
        var sb = new StringBuilder();
        var spans = new List<PartSpan>();
        var ends = new List<ParagraphEnd>();
        var paraIndex = 0;

        foreach (var para in shape.Paragraph)
        {
            foreach (var part in para.TextParts)
            {
                var text = part.Text;
                if (text.Length > 0)
                    spans.Add(new PartSpan(part, sb.Length, text.Length));
                sb.Append(text);
            }

            ends.Add(new ParagraphEnd(paraIndex, sb.Length));
            paraIndex++;
        }

        return (sb.ToString(), spans, ends);
    }

    #endregion

    #region Step 2: Two-pointer segment scan

    /// <summary>
    /// Scans the input string for contiguous segments and categorizes them
    /// based on the content and structure of the text, such as static text
    /// or tags.
    /// </summary>
    /// <param name="flat">The input string to scan and segmentize.</param>
    /// <returns>A list of segments, where each segment contains metadata
    /// like start position, length, content, and categorization (e.g.,
    /// static text or tag).</returns>
    private static List<FlatSegment> ScanSegments(string flat)
    {
        var segments = new List<FlatSegment>();
        var i = 0;
        var lastEnd = 0;

        while (i < flat.Length)
        {
            if (flat[i] != '{' || i + 1 >= flat.Length || flat[i + 1] != '{')
            {
                i++;
                continue;
            }

            if (i > lastEnd)
                segments.Add(new FlatSegment(lastEnd, i - lastEnd, flat[lastEnd..i], SegmentKind.StaticText));

            var isTriple = i + 2 < flat.Length && flat[i + 2] == '{';
            var closing = isTriple ? "}}}" : "}}";
            var closeIdx = flat.IndexOf(closing, i + (isTriple ? 3 : 2), StringComparison.Ordinal);

            if (closeIdx < 0)
            {
                i++;
                continue;
            }

            var tagEnd = closeIdx + closing.Length;
            var content = flat[i..tagEnd];
            segments.Add(new FlatSegment(i, tagEnd - i, content, ClassifyTag(content)));
            lastEnd = i = tagEnd;
        }

        if (lastEnd < flat.Length)
            segments.Add(new FlatSegment(lastEnd, flat.Length - lastEnd, flat[lastEnd..], SegmentKind.StaticText));

        return segments;
    }

    /// <summary>
    /// Classifies the type of tag based on its content.
    /// </summary>
    /// <param name="tagContent">The raw content of the tag, including surrounding braces.</param>
    /// <returns>A <see cref="SegmentKind"/> enum value indicating the type of the tag: either
    /// <see cref="SegmentKind.StructuralTag"/> or <see cref="SegmentKind.RenderableTag"/>.</returns>
    private static SegmentKind ClassifyTag(string tagContent)
    {
        var inner = tagContent.TrimStart('{').TrimEnd('}').Trim();
        return inner.StartsWith('#') || inner.StartsWith('^') || inner.StartsWith('/')
            ? SegmentKind.StructuralTag
            : SegmentKind.RenderableTag;
    }

    #endregion

    #region Step 3: Coverage ratio

    /// <summary>
    /// Calculates the coverage of text parts within a specified segment of a string.
    /// </summary>
    /// <param name="segStart">The starting position of the segment.</param>
    /// <param name="segLength">The length of the segment.</param>
    /// <param name="partSpans">A collection of text part spans representing the positions and lengths of text parts.</param>
    /// <returns>A list of coverage entries representing the fractional ownership of the segment by each text part.</returns>
    private static List<CoverageEntry> CalculateCoverage(
        int segStart, int segLength, IReadOnlyList<PartSpan> partSpans)
    {
        return (from span in partSpans
            let overlapStart = Math.Max(segStart, span.GlobalStart)
            let overlapEnd = Math.Min(segStart + segLength, span.GlobalStart + span.Length)
            where overlapEnd > overlapStart
            let ratio = (double)(overlapEnd - overlapStart) / segLength
            select new CoverageEntry(span.Source, ratio)).ToList();
    }

    #endregion

    #region Step 4: Build marked template

    /// <summary>
    /// Constructs a marked template string and a corresponding marker map based on the flattened shape,
    /// detected segments, part spans, and paragraph ends. The marked template replaces segment contents
    /// with uniquely identifiable markers, allowing later reconstruction of text parts.
    /// </summary>
    /// <param name="flat">The flattened string representation of the shape's content.</param>
    /// <param name="segments">A list of contiguous segments detected in the flattened string.</param>
    /// <param name="partSpans">A list of part spans indicating the mapping of text parts to their positions in the flattened string.</param>
    /// <param name="paraEnds">A list of positions marking the ends of paragraphs in the flattened string.</param>
    /// <returns>
    /// A tuple containing the marked template string and a dictionary mapping unique marker IDs
    /// to their respective coverage entries, which represent the ownership of text parts over specific segments.
    /// </returns>
    private static (string MarkedTemplate, Dictionary<string, List<CoverageEntry>> MarkerMap)
        BuildMarkedTemplate(
            string flat,
            IReadOnlyList<FlatSegment> segments,
            IReadOnlyList<PartSpan> partSpans,
            IReadOnlyList<ParagraphEnd> paraEnds)
    {
        var sb = new StringBuilder();
        var markerMap = new Dictionary<string, List<CoverageEntry>>();
        var pbIdx = 0;

        foreach (var seg in segments)
        {
            FlushBreaks(seg.Start);

            if (seg.Kind == SegmentKind.StructuralTag)
            {
                sb.Append(seg.Content);
                continue;
            }

            var id = $"m{Guid.NewGuid():N}";
            markerMap[id] = CalculateCoverage(seg.Start, seg.Length, partSpans);
            sb.Append('<').Append(id).Append('>')
                .Append(seg.Content)
                .Append("</").Append(id).Append('>');
        }

        FlushBreaks(flat.Length);
        return (sb.ToString(), markerMap);

        void FlushBreaks(int upToPosition)
        {
            while (pbIdx < paraEnds.Count && paraEnds[pbIdx].Position <= upToPosition)
                sb.Append($"<pb_{paraEnds[pbIdx++].ParagraphIndex}/>");
        }
    }

    #endregion

    #region Step 5: Parse rendered output

    /// <summary>
    /// Parses the rendered text into chunks by splitting on paragraph-break sentinels.
    /// Only sentinels with a valid paragraph index and not nested inside a known marker block
    /// are treated as real breaks — any text that merely looks like a sentinel is ignored.
    /// </summary>
    /// <param name="rendered">The rendered text string to be parsed into chunks.</param>
    /// <param name="markerMap">The map of known marker IDs used to locate valid marker ranges.</param>
    /// <param name="validParaIndices">The set of paragraph indices that were actually injected as sentinels.</param>
    /// <returns>A list of tuples, where each tuple contains the paragraph index and the corresponding text chunk.</returns>
    private static List<(int ParaIndex, string Chunk)> ParseChunks(
        string rendered,
        IReadOnlyDictionary<string, List<CoverageEntry>> markerMap,
        HashSet<int> validParaIndices)
    {
        // Scan only real marker ranges (same logic as ParseChunkMarkers) so that
        // <pb_N/> sentinels nested inside marker content are never treated as breaks.
        var validMarkerRanges = ScanValidMarkerRanges(rendered, markerMap);

        var result = new List<(int, string)>();
        var lastEnd = 0;

        foreach (Match match in ParagraphBreakRegex.Matches(rendered))
        {
            var paraIndex = int.Parse(match.Groups[1].Value);
            if (!validParaIndices.Contains(paraIndex)) continue;   // fake index
            if (IsInsideValidMarker(match.Index)) continue;        // nested in marker content

            result.Add((paraIndex, rendered[lastEnd..match.Index]));
            lastEnd = match.Index + match.Length;
        }

        return result;

        bool IsInsideValidMarker(int pos)
        {
            foreach (var (start, end) in validMarkerRanges)
                if (pos >= start && pos < end) return true;
            return false;
        }
    }

    /// <summary>
    /// Returns the start/end character ranges of every valid marker block in <paramref name="text"/>.
    /// Uses the same linear scan as <see cref="ParseChunkMarkers"/> — no broad regex.
    /// </summary>
    private static List<(int Start, int End)> ScanValidMarkerRanges(
        string text,
        IReadOnlyDictionary<string, List<CoverageEntry>> markerMap)
    {
        const int openTagLength = 35;
        var ranges = new List<(int, int)>();
        var i = 0;

        while (i < text.Length)
        {
            if (text[i] != '<' || i + openTagLength > text.Length || text[i + 1] != 'm'
                || !IsHex32(text, i + 2) || text[i + 34] != '>')
            {
                i++;
                continue;
            }

            var potentialId = string.Concat("m", text.AsSpan(i + 2, 32));
            if (!markerMap.ContainsKey(potentialId)) { i++; continue; }

            var closingTag = $"</{potentialId}>";
            var contentStart = i + openTagLength;
            var closeIdx = text.IndexOf(closingTag, contentStart, StringComparison.Ordinal);

            if (closeIdx < 0) { i++; continue; }

            var end = closeIdx + closingTag.Length;
            ranges.Add((i, end));
            i = end;
        }

        return ranges;
    }

    /// <summary>Returns true if the 32 characters starting at <paramref name="start"/> are all lowercase hex digits.</summary>
    private static bool IsHex32(string text, int start)
    {
        for (var k = start; k < start + 32; k++)
        {
            var c = text[k];
            if (c is (< '0' or > '9') and (< 'a' or > 'f')) return false;
        }
        return true;
    }

    #endregion

    #region Step 6: Reconstruct

    /// <summary>
    /// Divides the specified content into segments based on the provided coverage ratios,
    /// associating each segment with its corresponding text part source.
    /// </summary>
    /// <param name="content">The text content to be split into segments.</param>
    /// <param name="coverages">A list of coverage entries defining the ratio and source for each segment.</param>
    /// <returns>A list of tuples where each tuple contains a text part of the source and the corresponding segment of the text content.</returns>
    private static List<(ITextPart Source, string Text)> SplitByRatio(
        string content, IReadOnlyList<CoverageEntry> coverages)
    {
        var result = new List<(ITextPart, string)>(coverages.Count);
        var total = content.Length;
        var allocated = 0;

        for (var i = 0; i < coverages.Count; i++)
        {
            var charCount = i == coverages.Count - 1
                ? total - allocated
                : (int)Math.Round(coverages[i].Ratio * total);
            result.Add((coverages[i].Source, content.Substring(allocated, charCount)));
            allocated += charCount;
        }

        return result;
    }

    /// <summary>
    /// Reconstructs the shape by mapping processed text chunks back to paragraphs and markers within the shape.
    /// </summary>
    /// <param name="shape">
    /// The shape to be reconstructed. Represents the target of the reconstruction process and will be modified
    /// by adding new paragraphs based on the provided chunks.
    /// </param>
    /// <param name="chunks">
    /// A read-only list of tuples where each tuple contains a paragraph index and a corresponding chunk of processed text.
    /// These chunks represent the text content to be added back to paragraphs in the shape.
    /// </param>
    /// <param name="markerMap">
    /// A dictionary containing marker entries, where the key is a string identifying a marker and the value is a list of
    /// coverage entries. Each coverage entry maps original text parts to their fractional ownership in the marker segments.
    /// </param>
    private static void ReconstructShape(
        IShape shape,
        IReadOnlyList<(int ParaIndex, string Chunk)> chunks,
        Dictionary<string, List<CoverageEntry>> markerMap)
    {
        shape.ClearParagraph();

        foreach (var (_, chunk) in chunks)
        {
            IParagraph? para = null;

            foreach (var (id, content) in ParseChunkMarkers(chunk, markerMap))
            {
                if (content.Length == 0) continue;
                para ??= shape.AddParagraph();

                foreach (var (source, text) in SplitByRatio(content, markerMap[id]))
                {
                    if (text.Length == 0) continue;
                    var newPart = para.AddTextPart(source);
                    newPart.Text = text;
                }
            }
        }
    }

    /// <summary>
    /// Linearly scans a chunk string and yields only marker blocks whose IDs exist in
    /// <paramref name="markerMap"/>. Text that merely looks like a marker but has an unknown
    /// ID is treated as literal content and skipped transparently.
    /// </summary>
    private static IEnumerable<(string Id, string Content)> ParseChunkMarkers(
        string chunk,
        IReadOnlyDictionary<string, List<CoverageEntry>> markerMap)
    {
        // Opening tag layout: '<' + 'm' + 32 hex chars + '>' = 35 characters.
        const int openTagLength = 35;
        var i = 0;

        while (i < chunk.Length)
        {
            if (chunk[i] != '<' || i + openTagLength > chunk.Length || chunk[i + 1] != 'm')
            {
                i++;
                continue;
            }

            // Extract the candidate ID ("m" + 32 hex chars) only after validating hex digits.
            if (!IsHex32(chunk, i + 2) || chunk[i + 34] != '>') { i++; continue; }
            var potentialId = string.Concat("m", chunk.AsSpan(i + 2, 32));

            if (!markerMap.ContainsKey(potentialId))
            {
                i++;
                continue;
            }

            var contentStart = i + openTagLength;
            var closingTag = $"</{potentialId}>";
            var closeIdx = chunk.IndexOf(closingTag, contentStart, StringComparison.Ordinal);

            if (closeIdx < 0) { i++; continue; }

            yield return (potentialId, chunk[contentStart..closeIdx]);
            i = closeIdx + closingTag.Length;
        }
    }

    #endregion
}





