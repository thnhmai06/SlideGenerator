namespace SlideGenerator.Services.Generating.Models.Identifiers;

public record BookIdentifier(string BookPath, string? BookPassword = null)
{
    public string BookPath
    {
        get;
        init => field = Path.GetFullPath(value);
    } = BookPath;
}