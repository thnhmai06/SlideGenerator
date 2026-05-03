namespace SlideGenerator.Services.Generating.Models.Identifiers;

public record ColumnIdentifier(string BookFilePath, string SheetName, string ColumnName, string? BookPassword = null)
    : SheetIdentifier(BookFilePath, SheetName, BookPassword);