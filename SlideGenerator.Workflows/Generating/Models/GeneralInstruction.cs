namespace SlideGenerator.Workflows.Generating.Models;

public sealed record GeneralInstruction(
    string TargetShapeName,
    ICollection<string> SourceColumns,
    EditOptions Edit)
{
    public IEnumerable<SpecializedInstruction> Flatten(
        IReadOnlyDictionary<string, string> rowContent)
    {
        return SourceColumns.Select(columnName => new SpecializedInstruction(
            TargetShapeName,
            Utilities.NormalizeUri(rowContent.GetValueOrDefault(columnName)),
            Edit,
            columnName));
    }
}