namespace SlideGenerator.Services.Generating.Models.Identifiers;

public record PresentationIdentifier(string PresentationPath, string? PresentationPassword = null)
{
    public string PresentationPath
    {
        get;
        init => field = Path.GetFullPath(value);
    } = PresentationPath;
}