using System.Diagnostics.CodeAnalysis;
using SlideGenerator.Application.Services.Generating.Models.Images;
using ImageGeneral = SlideGenerator.Application.Services.Generating.Models.Images.GeneralInstruction;
using TextGeneral = SlideGenerator.Application.Services.Generating.Models.Texts.GeneralInstruction;
using SpecializedText = SlideGenerator.Application.Services.Generating.Models.Texts.SpecializedInstruction;

namespace SlideGenerator.Application.Services.Generating.Services;

/// <summary>
///     Provides services to resolve general instructions into specialized instructions containing actual values from row
///     data.
/// </summary>
public static class InstructionResolver
{
    /// <summary>
    ///     Resolves a general text instruction into a specialized one by picking the first available non-empty source value.
    /// </summary>
    /// <param name="general">The general text instruction containing potential sources.</param>
    /// <param name="rowContent">The row data containing column values.</param>
    /// <returns>A specialized instruction with the resolved string value.</returns>
    public static SpecializedText ResolveText(
        TextGeneral general,
        IReadOnlyDictionary<string, string> rowContent)
    {
        var specializedInstructions = general.Flatten(general, rowContent).ToList();
        return specializedInstructions.FirstOrDefault(
            x => !string.IsNullOrWhiteSpace(x.Value),
            specializedInstructions.First());
    }

    /// <summary>
    ///     Attempts to resolve a general image instruction into a specialized one by picking the first source that resolves to
    ///     a valid URI.
    /// </summary>
    /// <param name="general">The general image instruction containing potential URI sources.</param>
    /// <param name="rowContent">The row data containing column values.</param>
    /// <param name="result">
    ///     When this method returns <see langword="true" />, contains the specialized instruction with a
    ///     non-null URI; otherwise, <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> if a valid image URI was resolved; otherwise, <see langword="false" />.</returns>
    public static bool TryResolveImage(
        ImageGeneral general,
        IReadOnlyDictionary<string, string> rowContent,
        [NotNullWhen(true)] out SpecializedInstruction? result)
    {
        var candidates = general.Flatten(general, rowContent);

        result = candidates.FirstOrDefault(x => x.Value != null);
        return result != null;
    }
}