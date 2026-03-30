using SlideGenerator.Domain.Sheet.Models;

namespace SlideGenerator.Domain.Tasks.Models.Text;

/// <summary>
///     Represents a text binding configuration for replacement.
/// </summary>
/// <param name="Sources">Candidate columns used to resolve replacement value.</param>
public sealed record GeneralInstruction(string Placeholder, IReadOnlyList<ColumnIdentifier> Sources)
    : Instruction(Placeholder), ISpecializable<GeneralInstruction, SpecializedInstruction>
{
    public IEnumerable<SpecializedInstruction> Flatten(GeneralInstruction general)
        => general.Sources.Select(source => new SpecializedInstruction(general.Placeholder, source));
}