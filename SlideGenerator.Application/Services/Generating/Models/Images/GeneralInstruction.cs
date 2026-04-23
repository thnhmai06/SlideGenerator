using SlideGenerator.Domain.Sheets.Models;
using SlideGenerator.Domain.Slides.Models.Identifiers;

namespace SlideGenerator.Application.Services.Generating.Models.Images;

/// <summary>
///     Represents an image binding configuration for replacement.
/// </summary>
/// <param name="Target">The shape wants to replace.</param>
/// <param name="Sources">Sources to provides image URL, usually column names in the data source.</param>
/// <param name="Edit">ROI mode used for image crop and placement.</param>
public sealed record GeneralInstruction(
    ShapeIdentifier Target,
    IReadOnlyList<ColumnIdentifier> Sources,
    EditOptions Edit)
    : Instruction(Target, Edit), ISpecializable<GeneralInstruction, SpecializedInstruction>
{
    public IEnumerable<SpecializedInstruction> Flatten(GeneralInstruction general)
    {
        return general.Sources.Select(source => new SpecializedInstruction(general.Target, source, general.Edit));
    }
}