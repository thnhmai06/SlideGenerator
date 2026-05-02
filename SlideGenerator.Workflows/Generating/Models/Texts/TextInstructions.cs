namespace SlideGenerator.Workflows.Generating.Models.Texts;

public record SpecializedInstruction(string Placeholder, string Value);

public sealed record GeneralInstruction(string Placeholder, IReadOnlyList<string> SourceColumns)
{
    public SpecializedInstruction Empty => new(Placeholder, string.Empty);

    public IEnumerable<SpecializedInstruction> Flatten(IReadOnlyDictionary<string, string> rowContent)
    {
        return SourceColumns.Select(sourceName => new SpecializedInstruction(
            Placeholder,
            rowContent.TryGetValue(sourceName, out var value) ? value : string.Empty));
    }
}