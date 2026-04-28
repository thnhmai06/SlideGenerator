using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Generating.Models.Images;

/// <summary>
///     Represents a general image binding configuration that can be resolved into a specialized instruction.
/// </summary>
/// <param name="Target">The target shape to replace.</param>
/// <param name="Sources">The ordered list of candidate columns providing the image URL.</param>
/// <param name="Edit">The editing options for the image.</param>
public sealed record GeneralInstruction(
    ShapeIdentifier Target,
    ICollection<ColumnIdentifier> Sources,
    EditOptions Edit)
    : Instruction(Target, Edit), ISpecializable<GeneralInstruction, SpecializedInstruction>
{
    /// <inheritdoc />
    public IEnumerable<SpecializedInstruction> Flatten(
        GeneralInstruction general,
        IReadOnlyDictionary<string, string> rowContent)
    {
        return general.Sources.Select(source => new SpecializedInstruction(
            general.Target,
            Utilities.NormalizeUri(rowContent.GetValueOrDefault(source.Name)),
            general.Edit,
            source.Name));
    }
}