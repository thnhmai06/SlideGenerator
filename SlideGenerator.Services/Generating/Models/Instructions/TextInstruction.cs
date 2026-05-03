namespace SlideGenerator.Services.Generating.Models.Instructions;

public record TextInstruction(
    IReadOnlySet<string> Placeholders,
    IReadOnlyList<string> ColumnNames);