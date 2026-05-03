namespace SlideGenerator.Services.Generating.Models.Identifiers;

public record ColumnIdentifier(string BookPath, string SheetName, string ColumnName, string? BookPassword = null)
    : SheetIdentifier(BookPath, SheetName, BookPassword);