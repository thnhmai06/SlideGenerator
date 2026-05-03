namespace SlideGenerator.Services.Generating.Models.Identifiers;

public record SheetIdentifier(string BookPath, string SheetName, string? BookPassword = null)
    : BookIdentifier(BookPath, BookPassword);