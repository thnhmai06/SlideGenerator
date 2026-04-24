using SlideGenerator.Domain.Sheets.Models;

namespace SlideGenerator.Application.Services.Generating.Models.Texts;

/// <summary>
///     Represents a general text binding configuration that can be resolved into a specialized instruction.
/// </summary>
/// <param name="Placeholder">The placeholder string to find and replace.</param>
/// <param name="Sources">The ordered list of candidate columns used to resolve the replacement value.</param>
public sealed record GeneralInstruction(string Placeholder, IReadOnlyList<ColumnIdentifier> Sources)
    : Instruction(Placeholder), ISpecializable<GeneralInstruction, SpecializedInstruction>
{
    /// <inheritdoc />
    public IEnumerable<SpecializedInstruction> Flatten(GeneralInstruction general,
        IReadOnlyDictionary<string, string> rowContent)
    {
        return general.Sources.Select(source => new SpecializedInstruction(
            general.Placeholder,
            rowContent.TryGetValue(source.Name, out var value) ? value : string.Empty));
    }
}