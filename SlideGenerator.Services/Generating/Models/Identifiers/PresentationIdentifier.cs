namespace SlideGenerator.Services.Generating.Models.Identifiers;

public record PresentationIdentifier(string PresentationFilePath, string? PresentationPassword = null)
{
    public string PresentationFilePath
    {
        get;
        init => field = Path.GetFullPath(value);
    } = PresentationFilePath;
}