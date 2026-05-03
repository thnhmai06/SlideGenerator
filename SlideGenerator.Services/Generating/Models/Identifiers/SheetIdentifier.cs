namespace SlideGenerator.Services.Generating.Models.Identifiers;

public sealed record SheetIdentifier(string BookFilePath, string SheetName, string? BookPassword = null)
    : BookIdentifier(BookFilePath, BookPassword);