using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Application.Workflows.Generating.Models.Texts;

/// <summary>
///     Represents a text binding configuration for replacement.
/// </summary>
/// <param name="Sources">Candidate columns used to resolve replacement value.</param>
public sealed record GeneralInstruction(string Placeholder, IReadOnlyList<ColumnIdentifier> Sources)
    : Instruction(Placeholder), ISpecializable<GeneralInstruction, SpecializedInstruction>
{
    public IEnumerable<SpecializedInstruction> Flatten(GeneralInstruction general)
    {
        return general.Sources.Select(source => new SpecializedInstruction(general.Placeholder, source));
    }
}