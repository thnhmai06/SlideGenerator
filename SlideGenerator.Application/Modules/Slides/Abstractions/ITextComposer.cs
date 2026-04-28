using SlideGenerator.Domain.Slides.Entities.Shape;

namespace SlideGenerator.Application.Modules.Slides.Abstractions;

/// <summary>
///     Defines a contract for scanning and replacing text content within a slide shape.
/// </summary>
public interface ITextComposer
{
    /// <summary>
    ///     Scans the provided read-only shape to extract text segments or placeholders.
    /// </summary>
    /// <param name="shape">The <see cref="IReadOnlyShape" /> to scan.</param>
    /// <returns>An enumeration of text strings found within the shape.</returns>
    IEnumerable<string> Scan(IReadOnlyShape shape);

    /// <summary>
    ///     Replaces text content in the specified shape based on a dictionary of instructions.
    /// </summary>
    /// <param name="shape">The editable <see cref="IShape" /> to modify.</param>
    /// <param name="instructions">A mapping of placeholder strings to their replacement values.</param>
    /// <returns>An integer representing the number of replacements made or a status code.</returns>
    int Replace(IShape shape, IReadOnlyDictionary<string, string> instructions);
}