namespace SlideGenerator.Services.Generating.Models.Identifiers;

public record BookIdentifier(string BookFilePath, string? BookPassword = null)
{
    public string BookFilePath
    {
        get;
        init => field = Path.GetFullPath(value);
    } = BookFilePath;
}